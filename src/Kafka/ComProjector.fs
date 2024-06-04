namespace UniStream.Domain

open System.Threading
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


type ComProjector<'agg when 'agg :> Aggregate>
    (
        logger: ILogger<ComProjector<'agg>>,
        producer: IProducer<string, byte array>,
        consumer: IConsumer<string, byte array>
    ) =
    let p = producer.Client
    let c = consumer.Client
    let aggType = typeof<'agg>.FullName
    let topic = aggType + "_Reply"

    let work (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    p.Produce(topic, Message<string, byte array>(Key = cr.Message.Key, Value = [||]))
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    interface IWorker with
        member _.Launch(ct: CancellationToken) =
            task {
                c.Subscribe(aggType)
                Async.Start(work ct, ct)
                logger.LogInformation($"Subscribe {aggType} started")
            }
