namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open EventStore.Client


module Worker =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (logger: ILogger)
        (client: IClient)
        (f: Guid option -> Guid -> 'com -> Async<'agg>)
        =

        let agent =
            MailboxProcessor<ResolvedEvent>.Start
            <| fun inbox ->
                let rec loop () =
                    async {
                        let! ev = inbox.Receive()
                        let meta = Encoding.ASCII.GetString ev.Event.Metadata.Span
                        let aggId = meta[19..54]
                        let aggType = typeof<'agg>.FullName
                        let stream = "Fail-" + aggType
                        let comId = ev.Event.EventId.ToGuid()
                        let comType = typeof<'com>.FullName
                        let com = JsonSerializer.Deserialize<'com> ev.Event.Data.Span

                        try
                            do! f (Some comId) (Guid aggId) com |> Async.Ignore
                            logger.LogInformation($"{comType} of {aggId} finished")
                        with ex ->
                            let metadata =
                                $"{{\"$correlationId\":\"{comId}\"}}"
                                |> Encoding.ASCII.GetBytes
                                |> ReadOnlyMemory
                                |> Nullable

                            logger.LogError($"{comType} of {aggId} error: {ex}")
                            let evtData = JsonSerializer.SerializeToUtf8Bytes ex.Message
                            let data = EventData(Uuid.NewUuid(), "Fail", evtData, metadata)
                            client.Client.AppendToStreamAsync(stream, StreamState.Any, [ data ]).Wait()

                        return! loop ()
                    }

                loop ()

        typeof<'com>.FullName, agent

    let inline run<'agg when 'agg :> Aggregate>
        (ct: CancellationToken)
        (logger: ILogger)
        (sub: ISubscriber)
        (group: string)
        (dic: IDictionary<string, MailboxProcessor<ResolvedEvent>>)
        : Tasks.Task =
        task {
            let stream = typeof<'agg>.FullName
            use sub = sub.Subscriber.SubscribeToStream(stream, group, cancellationToken = ct)
            let e = sub.Messages.GetAsyncEnumerator()

            while! e.MoveNextAsync().AsTask() do
                match e.Current with
                | :? PersistentSubscriptionMessage.Event as ev ->
                    try
                        dic[ev.ResolvedEvent.Event.EventType].Post ev.ResolvedEvent
                    with ex ->
                        logger.LogCritical($"{ex}")

                    sub.Ack(ev.ResolvedEvent) |> ignore
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation($"Subscription {confirm.SubscriptionId} for {stream} started")
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError("Stream was not found")
                | _ -> logger.LogError("Unknown error")
        }
