namespace UniStream.Domain

open System
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open EventStore.Client


module Worker =

    let inline send<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (client: IClient) (aggId: Guid) (com: 'com) =
        async {
            let mutable s = true
            let client = client.Client
            let comData = JsonSerializer.SerializeToUtf8Bytes com
            let comId = Uuid.NewUuid()
            let data = EventData(comId, typeof<'com>.FullName, comData)

            try
                client.AppendToStreamAsync(aggId.ToString(), StreamState.Any, [ data ]).Wait()
                use sub = client.SubscribeToStream($"$bc-{comId}", FromStream.Start, true)
                let e = sub.Messages.GetAsyncEnumerator()

                while s do
                    e.MoveNextAsync().AsTask().Wait()

                    match e.Current with
                    | :? StreamMessage.Event -> s <- false
                    | :? StreamMessage.NotFound -> s <- false
                    | :? StreamMessage.Unknown -> s <- false
                    | _ -> ()

                match e.Current with
                | :? StreamMessage.Event as ev ->
                    if ev.ResolvedEvent.Event.EventType = typeof<'evt>.FullName then
                        return ()
                    elif ev.ResolvedEvent.Event.EventType = "Fail" then
                        let err = JsonSerializer.Deserialize<string> ev.ResolvedEvent.Event.Data.Span
                        return failwith $"Apply command failed: {err}"
                    else
                        return failwith $"Wrong event type: {ev.ResolvedEvent.Event.EventType}"
                | ev -> return failwith $"Unexpected stream message type: {ev}"
            with ex ->
                return raise ex
        }

    let inline work<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (logger: ILogger)
        (client: IClient)
        (ev: ResolvedEvent)
        (stream: string)
        (f: Guid option -> Guid -> 'com -> Async<'agg>)
        (ack: ResolvedEvent -> Tasks.Task)
        =
        async {
            let com = JsonSerializer.Deserialize<'com> ev.Event.Data.Span
            let comId = ev.Event.EventId.ToGuid()
            let aggId = ev.Event.EventStreamId

            try
                logger.LogInformation($"Receive command of {aggId} from {stream}")
                f (Some comId) (Guid aggId) com |> Async.RunSynchronously |> ignore
            with ex ->
                let metadata =
                    $"{{\"$correlationId\":\"{comId}\"}}"
                    |> Encoding.ASCII.GetBytes
                    |> ReadOnlyMemory
                    |> Nullable

                logger.LogError($"Handle command of {stream} error: {ex}")
                let evtData = JsonSerializer.SerializeToUtf8Bytes ex.Message
                let data = EventData(Uuid.NewUuid(), "Fail", evtData, metadata)
                client.Client.AppendToStreamAsync(aggId, StreamState.Any, [ data ]).Wait()

            ack ev |> ignore
        }

    let inline run<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (ct: CancellationToken)
        (logger: ILogger)
        (client: IClient)
        (sub: ISubscriber)
        (group: string)
        (f: Guid option -> Guid -> 'com -> Async<'agg>)
        : Tasks.Task =
        task {
            let stream = "$et-" + typeof<'com>.FullName
            use sub = sub.Subscriber.SubscribeToStream(stream, group, cancellationToken = ct)
            let e = sub.Messages.GetAsyncEnumerator()

            while! e.MoveNextAsync().AsTask() do
                match e.Current with
                | :? PersistentSubscriptionMessage.Event as ev ->
                    work logger client ev.ResolvedEvent stream f sub.Ack |> Async.StartImmediate
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation($"Subscription {confirm.SubscriptionId} started")
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError("Stream was not found")
                | _ -> logger.LogError("Unknown error")
        }
