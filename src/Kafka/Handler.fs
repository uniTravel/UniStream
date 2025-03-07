namespace UniStream.Domain

open System
open System.Text
open System.Text.Json
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


module Handler =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (subscriber: ISubscriber<'agg>)
        (logger: ILogger)
        (tp: IProducer)
        (commit: Guid -> Guid -> 'com -> Async<ComResult>)
        =
        let aggType = typeof<'agg>.FullName
        let comType = typeof<'com>.FullName
        let tp = tp.Client

        let agent =
            new MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>(fun inbox ->
                let rec loop () =
                    async {
                        let! aggId, comId, comData = inbox.Receive()
                        let com = JsonSerializer.Deserialize<'com> comData.Span

                        match! commit aggId comId com with
                        | Success -> logger.LogInformation $"{comType} of {aggId} committed"
                        | Duplicate ->
                            let aggId = aggId.ToByteArray()
                            let h = Headers()
                            let msg = Message<byte array, byte array>(Key = aggId, Value = [||], Headers = h)
                            msg.Headers.Add("comId", comId.ToByteArray())
                            msg.Headers.Add("evtType", Encoding.ASCII.GetBytes "Duplicate")
                            tp.Produce(aggType, msg)
                            logger.LogWarning $"{comType} of {aggId} duplicated"
                        | Fail ex ->
                            let evtData = JsonSerializer.SerializeToUtf8Bytes ex.Message
                            let aggId = aggId.ToByteArray()
                            let h = Headers()
                            let msg = Message<byte array, byte array>(Key = aggId, Value = evtData, Headers = h)
                            msg.Headers.Add("comId", comId.ToByteArray())
                            msg.Headers.Add("evtType", Encoding.ASCII.GetBytes "Fail")
                            tp.Produce(aggType, msg)
                            logger.LogError $"{comType} of {aggId} error: {ex}"

                        return! loop ()
                    }

                loop ())

        subscriber.AddHandler comType agent
        agent.Start()
