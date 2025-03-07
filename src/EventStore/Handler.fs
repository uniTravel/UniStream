namespace UniStream.Domain

open System
open System.Text.Json
open Microsoft.Extensions.Logging
open EventStore.Client
open UniStream.Domain


module Handler =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (subscriber: ISubscriber<'agg>)
        (logger: ILogger)
        (client: IClient)
        (commit: Guid -> Guid -> 'com -> Async<ComResult>)
        =
        let aggType = typeof<'agg>.FullName
        let comType = typeof<'com>.FullName
        let client = client.Client
        let fail = aggType + "-Fail"
        let duplicate = aggType + "-Duplicate"

        let agent =
            new MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>(fun inbox ->
                let rec loop () =
                    async {
                        let! aggId, comId, evtData = inbox.Receive()
                        let com = JsonSerializer.Deserialize<'com> evtData.Span

                        match! commit aggId comId com with
                        | Success -> logger.LogInformation $"{comType} of {aggId} committed"
                        | Duplicate ->
                            let data = EventData(Uuid.FromGuid comId, "Duplicate", ReadOnlyMemory [||])

                            client.AppendToStreamAsync(duplicate, StreamState.Any, [ data ]).Wait()
                            logger.LogWarning $"{comType} of {aggId} duplicated"
                        | Fail ex ->
                            let data =
                                EventData(Uuid.FromGuid comId, "Fail", JsonSerializer.SerializeToUtf8Bytes ex.Message)

                            client.AppendToStreamAsync(fail, StreamState.Any, [ data ]).Wait()
                            logger.LogError $"{comType} of {aggId} error: {ex}"

                        return! loop ()
                    }

                loop ())

        subscriber.AddHandler comType agent
        agent.Start()
