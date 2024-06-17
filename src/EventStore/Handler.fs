namespace UniStream.Domain

open System
open System.Text.Json
open Microsoft.Extensions.Logging
open EventStore.Client
open UniStream.Domain


module Handler =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (subscriber: ISubscriber)
        (logger: ILogger)
        (client: IClient)
        (commit: Guid -> Guid -> 'com -> Async<unit>)
        =
        let aggType = typeof<'agg>.FullName
        let comType = typeof<'com>.FullName
        let client = client.Client
        let fail = aggType + "-Fail"

        let agent =
            new MailboxProcessor<Uuid * EventRecord>(fun inbox ->
                let rec loop () =
                    async {
                        let! comId, evt = inbox.Receive()
                        
                        let aggId = Guid.NewGuid()
                        let com = JsonSerializer.Deserialize<'com> evt.Data.Span

                        try
                            do! commit aggId (comId.ToGuid()) com
                            logger.LogInformation($"{comType} of {aggId} committed")
                        with ex ->
                            let data = EventData(comId, "Fail", JsonSerializer.SerializeToUtf8Bytes ex.Message)
                            client.AppendToStreamAsync(fail, StreamState.Any, [ data ]).Wait()
                            logger.LogError($"{comType} of {aggId} error: {ex}")

                        return! loop ()
                    }

                loop ())

        subscriber.AddHandler comType agent
        agent.Start()
