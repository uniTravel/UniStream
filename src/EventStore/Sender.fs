namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open EventStore.Client
open FSharp.Control


[<Sealed>]
type Sender<'agg when 'agg :> Aggregate>
    (
        logger: ILogger<Sender<'agg>>,
        cfg: IOptions<EventStoreOptions>,
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
    let group = cfg.Value.GroupName
    let cts = new CancellationTokenSource()
    let mutable dispose = false

    let agent =
        new MailboxProcessor<Msg>(fun inbox ->
            let rec loop
                (todo: Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>)
                (cache: Dictionary<Guid, DateTime * Result<unit, exn>>)
                =
                async {
                    match! inbox.Receive() with
                    | Send(aggId, comId, comType, comData, channel) ->
                        match comId with
                        | _ when cache.ContainsKey comId -> channel.Reply <| snd cache[comId]
                        | _ when todo.ContainsKey comId -> todo[comId] <- channel
                        | _ ->
                            try
                                let metadata =
                                    $"{{\"$correlationId\":\"{aggId}\"}}"
                                    |> Encoding.ASCII.GetBytes
                                    |> ReadOnlyMemory
                                    |> Nullable

                                let data = EventData(Uuid.FromGuid comId, comType, comData, metadata)
                                client.AppendToStreamAsync(aggType, StreamState.Any, [ data ]).Wait()
                                todo.Add(comId, channel)
                            with ex ->
                                channel.Reply <| Error ex
                    | Receive(comId, result) ->
                        if todo.ContainsKey comId then
                            todo[comId].Reply result
                            todo.Remove comId |> ignore

                        let expire = DateTime.UtcNow.AddMilliseconds interval
                        cache.Add(comId, (expire, result)) |> ignore
                    | Refresh(now) ->
                        cache
                        |> Seq.iter (fun (KeyValue(comId, (expire, _))) ->
                            if expire < now then
                                cache.Remove comId |> ignore)

                    return! loop todo cache
                }

            loop
                (Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>())
                (Dictionary<Guid, DateTime * Result<unit, exn>>()))

    let subscribe (ct: CancellationToken) =
        task {
            try
                sub.GetInfoToStreamAsync(stream, group).Wait()
            with _ ->
                let settings = PersistentSubscriptionSettings(true)
                sub.CreateToStreamAsync(stream, group, settings).Wait()

            use sub = sub.SubscribeToStream(stream, group, cancellationToken = ct)
            let e = sub.Messages.GetAsyncEnumerator()

            while! e.MoveNextAsync() do
                match e.Current with
                | :? PersistentSubscriptionMessage.Event as ev ->
                    match ev.ResolvedEvent.Event.EventType with
                    | "Fail" ->
                        let comId = ev.ResolvedEvent.Event.EventId.ToGuid()
                        let err = JsonSerializer.Deserialize<string> ev.ResolvedEvent.Event.Data.Span
                        logger.LogError($"{comId} of {aggType} failed: {err}")
                        agent.Post <| Receive(comId, Error(failwith $"Apply command failed: {err}"))
                        sub.Ack(ev.ResolvedEvent).Wait()
                    | _ ->
                        let comId = ev.ResolvedEvent.Event.EventId.ToGuid()
                        logger.LogInformation($"{comId} of {aggType} finished")
                        agent.Post <| Receive(comId, Ok())
                        sub.Ack(ev.ResolvedEvent).Wait()
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation($"Subscription {confirm.SubscriptionId} for {stream} started")
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError("Stream was not found")
                | _ -> logger.LogError("Unknown error")
        }

    do
        agent.Start()
        Async.Start(subscribe cts.Token |> Async.AwaitTask, cts.Token)
        Async.Start(Sender.timer interval (fun _ -> agent.Post <| Refresh(DateTime.UtcNow)), cts.Token)

    interface ISender<'agg> with
        member val send =
            fun aggId comId comType comData ->
                agent.PostAndAsyncReply(fun channel -> Send(aggId, comId, comType, comData, channel))

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                dispose <- true
