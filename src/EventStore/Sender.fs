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


type Msg =
    | Send of Guid * Uuid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of EventRecord
    | Refresh of DateTime


type ISender =

    abstract member Agent: MailboxProcessor<Msg>


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
    let interval = options.Interval * 1000
    let aggType = typeof<'agg>.FullName
    let stream = "$ce-" + aggType
    let group = cfg.Value.GroupName
    let cts = new CancellationTokenSource()
    let mutable dispose = false

    let agent =
        new MailboxProcessor<Msg>(fun inbox ->
            let todo = Dictionary<Uuid, AsyncReplyChannel<Result<unit, exn>>>()
            let cache = Dictionary<Uuid, DateTime * EventRecord>()

            let reply comId evtType (evtData: ReadOnlyMemory<byte>) =
                match evtType with
                | _ when evtType = "Fail" ->
                    let err = JsonSerializer.Deserialize<string> evtData.Span
                    logger.LogError($"{comId} of {aggType} failed: {err}")
                    todo[comId].Reply <| Error(failwith $"Apply command failed: {err}")
                | _ ->
                    logger.LogInformation($"{comId} of {aggType} finished")
                    todo[comId].Reply <| Ok()

            let rec loop () =
                async {
                    match! inbox.Receive() with
                    | Send(aggId, comId, comType, comData, channel) ->
                        match comId with
                        | _ when cache.ContainsKey comId ->
                            let _, er = cache[comId]
                            reply comId er.EventType er.Data
                            cache.Remove comId |> ignore
                        | _ when todo.ContainsKey comId -> todo[comId] <- channel
                        | _ ->
                            try
                                let metadata =
                                    $"{{\"$correlationId\":\"{aggId}\"}}"
                                    |> Encoding.ASCII.GetBytes
                                    |> ReadOnlyMemory
                                    |> Nullable

                                let data = EventData(comId, comType, comData, metadata)
                                client.AppendToStreamAsync(aggType, StreamState.Any, [ data ]).Wait()
                                todo.Add(comId, channel)
                            with ex ->
                                channel.Reply <| Error ex
                    | Receive(er) ->
                        match er.EventId with
                        | comId when todo.ContainsKey comId ->
                            reply comId er.EventType er.Data
                            todo.Remove comId |> ignore
                        | comId when not <| cache.ContainsKey comId ->
                            cache.Add(comId, (DateTime.UtcNow.AddMilliseconds interval, er)) |> ignore
                        | _ -> ()
                    | Refresh(now) ->
                        cache
                        |> Seq.iter (fun (KeyValue(comId, (expire, _))) ->
                            if expire < now then
                                cache.Remove comId |> ignore)

                    return! loop ()
                }

            loop ())

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
                    agent.Post <| Receive(ev.ResolvedEvent.Event)
                    sub.Ack(ev.ResolvedEvent).Wait()
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation($"Subscription {confirm.SubscriptionId} for {stream} started")
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError("Stream was not found")
                | _ -> logger.LogError("Unknown error")
        }

    let createTimer (interval: float) work =
        let timer = new Timers.Timer(interval)
        timer.AutoReset <- true
        timer.Elapsed.Add work
        async { timer.Start() }

    do
        agent.Start()
        Async.Start(subscribe cts.Token |> Async.AwaitTask, cts.Token)
        Async.Start(createTimer interval (fun _ -> agent.Post <| Refresh(DateTime.UtcNow)), cts.Token)

    interface ISender with
        member val Agent = agent

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                dispose <- true


module Sender =
    let inline send<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (sender: ISender)
        (aggId: Guid)
        (comId: Guid)
        (com: 'com)
        =
        async {
            match!
                sender.Agent.PostAndAsyncReply
                <| fun channel ->
                    Send(
                        aggId,
                        Uuid.FromGuid comId,
                        typeof<'com>.FullName,
                        JsonSerializer.SerializeToUtf8Bytes com,
                        channel
                    )
            with
            | Ok() -> return ()
            | Error ex -> return raise ex
        }
