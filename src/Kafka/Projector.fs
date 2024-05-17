namespace UniStream.Domain

open System
open System.Text
open System.Threading
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


type Projector<'agg when 'agg :> Aggregate>
    (logger: ILogger<Projector<'agg>>, producer: IProducer<string, byte array>, consumer: IConsumer<string, byte array>)
    =
    let p = producer.Client
    let c = consumer.Client
    let aggType = typeof<'agg>.FullName

    let work (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    let evtType = Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("evtType"))
                    let topic = aggType + "-" + cr.Message.Key
                    p.Produce(topic, Message<string, byte array>(Key = evtType, Value = cr.Message.Value))
                    p.Flush(TimeSpan.FromSeconds(10)) |> ignore
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
