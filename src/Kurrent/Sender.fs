namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open EventStore.Client


[<Sealed>]
type Sender<'agg when 'agg :> Aggregate>
    (
        logger: ILogger<Sender<'agg>>,
        config: IConfiguration,
        options: IOptionsMonitor<CommandOptions>,
        sub: IPersistent,
        client: IClient
    ) =
    let sub = sub.Subscriber
    let client = client.Client
    let options = options.Get(typeof<'agg>.Name)
    let interval = options.Interval * 60000
    let aggType = typeof<'agg>.FullName
    let stream = "$ce-" + aggType
    let hostname = config["Kurrent:Hostname"]
    let group = aggType + "-" + hostname
    let cts = new CancellationTokenSource()
    let mutable dispose = false

    let todo =
        (fun (inbox: MailboxProcessor<Backlog>) ->
            let rec loop (todo: Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>) =
                async {
                    match! inbox.Receive() with
                    | Add(comId, channel) ->
                        if todo.ContainsKey comId then
                            todo[comId] <- channel
                        else
                            todo.Add(comId, channel)
                    | Remove(comId, result) ->
                        if todo.ContainsKey comId then
                            todo[comId].Reply result
                            logger.LogInformation $"Apply [{comId}] of {aggType} completed"
                            todo.Remove comId |> ignore

                    return! loop todo
                }

            loop (Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>())
         , cts.Token)
        |> MailboxProcessor<Backlog>.Start

    let agent =
        (fun (inbox: MailboxProcessor<Msg>) ->
            let rec loop (cache: Dictionary<Guid, DateTime * Result<unit, exn>>) =
                async {
                    match! inbox.Receive() with
                    | Send(aggId, comId, comType, comData, channel) ->
                        todo.Post <| Add(comId, channel)

                        if cache.ContainsKey comId then
                            todo.Post <| Remove(comId, snd cache[comId])
                        else
                            try
                                let metadata =
                                    $"{{\"$correlationId\":\"{aggId}\"}}"
                                    |> Encoding.ASCII.GetBytes
                                    |> ReadOnlyMemory
                                    |> Nullable

                                let data = EventData(Uuid.FromGuid comId, comType, comData, metadata)

                                client.AppendToStreamAsync(aggType, StreamState.Any, [ data ])
                                |> Async.AwaitTask
                                |> Async.RunSynchronously
                                |> ignore
                            with ex ->
                                let ex = WriteException("Send command failed", ex)
                                logger.LogError(ex, $"Send {comType}[{comId}] of {aggType}[{aggId}] failed")
                                todo.Post <| Remove(comId, Error ex)
                    | Receive(comId, result) ->
                        todo.Post <| Remove(comId, result)
                        let expire = DateTime.UtcNow.AddMilliseconds interval
                        cache.TryAdd(comId, (expire, result)) |> ignore
                    | Refresh now ->
                        cache
                        |> Seq.iter (fun (KeyValue(comId, (expire, _))) ->
                            if expire < now then
                                cache.Remove comId |> ignore)

                    return! loop cache
                }

            loop (Dictionary<Guid, DateTime * Result<unit, exn>>())
         , cts.Token)
        |> MailboxProcessor<Msg>.Start

    let subscribe =
        task {
            try
                let! _ = sub.GetInfoToStreamAsync(stream, group)
                ()
            with _ ->
                let settings = PersistentSubscriptionSettings true
                do! sub.CreateToStreamAsync(stream, group, settings)

            use sub = sub.SubscribeToStream(stream, group, cancellationToken = cts.Token)
            use e = sub.Messages.GetAsyncEnumerator()

            while! e.MoveNextAsync() do
                match e.Current with
                | :? PersistentSubscriptionMessage.Event as ev ->
                    match ev.ResolvedEvent.Event.EventType with
                    | "Fail" ->
                        let comId = ev.ResolvedEvent.Event.EventId.ToGuid()
                        let err = JsonSerializer.Deserialize<string> ev.ResolvedEvent.Event.Data.Span
                        logger.LogError $"{comId} of {aggType} failed: {err}"
                        agent.Post <| Receive(comId, Error(failwith $"Apply command failed: {err}"))
                        do! sub.Ack ev.ResolvedEvent
                    | _ ->
                        let comId = ev.ResolvedEvent.Event.EventId.ToGuid()
                        logger.LogInformation $"{comId} of {aggType} finished"
                        agent.Post <| Receive(comId, Ok())
                        do! sub.Ack ev.ResolvedEvent
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation $"Subscription {confirm.SubscriptionId} for {stream} started"
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError "Stream was not found"
                | _ -> logger.LogError "Unknown error"
        }

    let refresh =
        async {
            do! Async.Sleep interval
            agent.Post <| Refresh DateTime.UtcNow
        }

    do
        agent.Start()
        Async.Start(refresh, cts.Token)
        subscribe.Start()

    interface ISender<'agg> with
        member val send =
            fun aggId comId comType comData ->
                agent.PostAndAsyncReply(fun channel -> Send(aggId, comId, comType, comData, channel))

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                todo.Dispose()
                dispose <- true
