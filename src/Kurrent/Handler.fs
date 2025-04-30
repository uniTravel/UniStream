namespace UniStream.Domain

open System
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open EventStore.Client
open UniStream.Domain


module Handler =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (subscriber: ISubscriber<'agg>)
        (logger: ILogger)
        (client: IClient)
        (ct: CancellationToken)
        (commit: Guid -> Guid -> 'com -> Async<ComResult>)
        =
        let aggType = typeof<'agg>.FullName
        let comType = typeof<'com>.FullName
        let client = client.Client

        let append (aggId: Guid) (comId: Guid) data stream =
            async {
                try
                    client.AppendToStreamAsync(stream, StreamState.Any, [ data ])
                    |> Async.AwaitTask
                    |> Async.RunSynchronously
                    |> ignore
                with ex ->
                    logger.LogError(ex, $"Reply {comType}[{comId}] of {aggType}[{aggId}] failed")
            }

        let agent =
            (fun (inbox: MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>) ->
                let rec loop () =
                    async {
                        let! aggId, comId, comData = inbox.Receive()
                        let com = JsonSerializer.Deserialize<'com> comData.Span

                        match! commit aggId comId com with
                        | Success -> logger.LogInformation $"Handle {comType}[{comId}] of {aggType}[{aggId}] success"
                        | Duplicate ->
                            logger.LogWarning $"Handle {comType}[{comId}] of {aggType}[{aggId}] duplicated"
                            let data = EventData(Uuid.FromGuid comId, "Duplicate", ReadOnlyMemory [||])
                            aggType + "-Duplicate" |> append aggId comId data |> Async.Start
                        | Fail ex ->
                            logger.LogError(ex, $"Handle {comType}[{comId}] of {aggType}[{aggId}] failed")
                            let evtData = JsonSerializer.SerializeToUtf8Bytes ex.Message
                            let data = EventData(Uuid.FromGuid comId, "Fail", evtData)
                            aggType + "-Fail" |> append aggId comId data |> Async.Start

                        return! loop ()
                    }

                loop ()
             , ct)
            |> MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>.Start

        subscriber.AddHandler comType agent
        logger.LogInformation $"Handler of {aggType} registered"
