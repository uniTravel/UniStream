namespace UniStream.Domain

open System
open System.Threading
open System.Text
open System.Text.Json
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


module Handler =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (subscriber: ISubscriber<'agg>)
        (logger: ILogger)
        (tp: IProducer<'agg>)
        (ct: CancellationToken)
        (commit: Guid -> Guid -> 'com -> Async<ComResult>)
        =
        let aggType = typeof<'agg>.FullName
        let comType = typeof<'com>.FullName
        let tp = tp.Client

        let delivery (aggId: Guid) (comType: string) (comId: Guid) (report: DeliveryReport<byte array, byte array>) =
            match report.Error.Code with
            | ErrorCode.NoError -> logger.LogInformation $"Reply {comType}[{comId}] of {aggType}[{aggId}] success"
            | err -> failwith <| err.GetReason()

        let produce aggId comId msg =
            try
                tp.Produce(aggType, msg, delivery aggId comType comId)
            with ex ->
                logger.LogError(ex, $"Reply {comType}[{comId}] of {aggType}[{aggId}] failed")

        let agent =
            (fun (inbox: MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>) ->
                let rec loop () =
                    async {
                        let! aggId, comId, comData = inbox.Receive()
                        let com = JsonSerializer.Deserialize<'com> comData.Span

                        match! commit aggId comId com with
                        | Success -> logger.LogInformation $"Handle {comType}[{comId}] of {aggType}[{aggId}] success"
                        | Duplicate ->
                            let aId = aggId.ToByteArray()
                            let h = Headers()
                            let msg = Message<byte array, byte array>(Key = aId, Value = [||], Headers = h)
                            msg.Headers.Add("comId", comId.ToByteArray())
                            msg.Headers.Add("evtType", Encoding.ASCII.GetBytes "Duplicate")
                            produce aggId comId msg
                            logger.LogWarning $"Handle {comType}[{comId}] of {aggType}[{aggId}] duplicated"
                        | Fail ex ->
                            let evtData = JsonSerializer.SerializeToUtf8Bytes ex.Message
                            let aId = aggId.ToByteArray()
                            let h = Headers()
                            let msg = Message<byte array, byte array>(Key = aId, Value = evtData, Headers = h)
                            msg.Headers.Add("comId", comId.ToByteArray())
                            msg.Headers.Add("evtType", Encoding.ASCII.GetBytes "Fail")
                            produce aggId comId msg
                            logger.LogError(ex, $"Handle {comType}[{comId}] of {aggType}[{aggId}] failed")

                        return! loop ()
                    }

                loop ()
             , ct)
            |> MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>.Start

        subscriber.AddHandler comType agent
        logger.LogInformation $"Handler of {aggType} registered"
