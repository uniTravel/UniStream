namespace UniStream.Domain

open System
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open EventStore.Client


module Worker =

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

            let agent =
                MailboxProcessor<ResolvedEvent>.Start
                <| fun inbox ->
                    let rec loop () =
                        async {
                            let! ev = inbox.Receive()
                            let com = JsonSerializer.Deserialize<'com> ev.Event.Data.Span
                            let comId = ev.Event.EventId.ToGuid()
                            let aggId = ev.Event.EventStreamId

                            try
                                do! f (Some comId) (Guid aggId) com |> Async.Ignore
                                logger.LogInformation($"{stream} of {aggId} finished")
                            with ex ->
                                let metadata =
                                    $"{{\"$correlationId\":\"{comId}\"}}"
                                    |> Encoding.ASCII.GetBytes
                                    |> ReadOnlyMemory
                                    |> Nullable

                                logger.LogError($"{stream} of {aggId} error: {ex}")
                                let evtData = JsonSerializer.SerializeToUtf8Bytes ex.Message
                                let data = EventData(Uuid.NewUuid(), "Fail", evtData, metadata)
                                client.Client.AppendToStreamAsync(aggId, StreamState.Any, [ data ]).Wait()

                            return! loop ()
                        }

                    loop ()

            while! e.MoveNextAsync().AsTask() do
                match e.Current with
                | :? PersistentSubscriptionMessage.Event as ev ->
                    agent.Post ev.ResolvedEvent
                    sub.Ack(ev.ResolvedEvent) |> ignore
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation($"Subscription {confirm.SubscriptionId} for {stream} started")
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError("Stream was not found")
                | _ -> logger.LogError("Unknown error")
        }
