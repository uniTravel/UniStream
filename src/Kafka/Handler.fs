namespace UniStream.Domain

open System
open System.Text
open System.Text.Json
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


module Handler =

    let inline register<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (subscriber: Subscriber<'agg>)
        (logger: ILogger)
        (producer: IProducer<Guid, byte array>)
        (commit: Guid option -> Guid -> 'com -> Async<'agg>)
        =
        let aggType = typeof<'agg>.FullName
        let comType = typeof<'com>.FullName
        let topic = aggType + "<:"
        let p = producer.Client

        let reply (tp: TopicPartition) comId (v: string) =
            p.Produce(tp, Message<Guid, byte array>(Key = comId, Value = Encoding.ASCII.GetBytes v))
            p.Flush(TimeSpan.FromSeconds(10)) |> ignore

        let agent =
            new MailboxProcessor<Guid * Guid * int * byte array>(fun inbox ->
                let rec loop () =
                    async {
                        let! aggId, comId, partition, comData = inbox.Receive()
                        let com = JsonSerializer.Deserialize<'com> comData
                        let tp = TopicPartition(topic, Partition partition)

                        try
                            do! commit None aggId com |> Async.Ignore
                            reply tp comId ""
                            logger.LogInformation($"{comType} of {aggId} finished")
                        with ex ->
                            reply tp comId ex.Message
                            logger.LogError($"{comType} of {aggId} error: {ex}")

                        return! loop ()
                    }

                loop ())

        subscriber.AddHandler typeof<'com>.FullName agent
        agent.Start()
