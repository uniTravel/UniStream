namespace UniStream.Domain

open System.Text
open System.Threading
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


[<Sealed>]
type Projector<'agg when 'agg :> Aggregate>(logger: ILogger<Projector<'agg>>, producer: IProducer, consumer: IConsumer)
    =
    let p = producer.Client
    let c = consumer.Client
    let aggType = typeof<'agg>.FullName

    let work (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    let aggId = Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("aggId"))
                    let evtType = cr.Message.Headers.GetLastBytes("evtType")

                    match Encoding.ASCII.GetString evtType with
                    | "Fail" -> ()
                    | _ ->
                        let topic = aggType + "-" + aggId
                        p.Produce(topic, Message<byte array, byte array>(Key = evtType, Value = cr.Message.Value))
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    interface IWorker<'agg> with
        member _.Launch(ct: CancellationToken) =
            task {
                c.Subscribe(aggType)
                Async.Start(work ct, ct)
                logger.LogInformation($"Subscription for {aggType} started")
            }
