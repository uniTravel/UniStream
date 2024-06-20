namespace UniStream.Domain

open System
open System.Text
open System.Text.Json
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


module Handler =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (subscriber: ISubscriber)
        (logger: ILogger)
        (producer: IProducer<string, byte array>)
        (commit: Guid -> Guid -> 'com -> Async<unit>)
        =
        let aggType = typeof<'agg>.FullName
        let comType = typeof<'com>.FullName
        let p = producer.Client

        let agent =
            new MailboxProcessor<string * string * byte array>(fun inbox ->
                let rec loop () =
                    async {
                        let! aggId, comId, comData = inbox.Receive()
                        let com = JsonSerializer.Deserialize<'com> comData

                        try
                            do! commit (Guid aggId) (Guid comId) com
                            logger.LogInformation($"{comType} of {aggId} committed")
                        with ex ->
                            let v = JsonSerializer.SerializeToUtf8Bytes ex.Message
                            let msg = Message<string, byte array>(Key = comId, Value = v, Headers = Headers())
                            msg.Headers.Add("evtType", Encoding.ASCII.GetBytes "Fail")
                            p.Produce(aggType, msg)
                            logger.LogError($"{comType} of {aggId} error: {ex}")

                        return! loop ()
                    }

                loop ())

        subscriber.AddHandler comType agent
        agent.Start()
