namespace UniStream.Domain

open System
open System.Text
open System.Threading
open Microsoft.Extensions.Logging
open Confluent.Kafka
open UniStream.Domain


[<Sealed>]
type Projector<'agg when 'agg :> Aggregate>(logger: ILogger<Projector<'agg>>, ap: IProducer, tc: IConsumer) =
    let ap = ap.Client
    let tc = tc.Client
    let aggType = typeof<'agg>.FullName

    let work (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = tc.Consume ct
                    let aggId = Guid cr.Message.Key
                    let evtType = cr.Message.Headers.GetLastBytes "evtType"

                    match Encoding.ASCII.GetString evtType with
                    | "Fail" -> ()
                    | _ ->
                        let topic = aggType + "-" + aggId.ToString()
                        ap.Produce(topic, Message<byte array, byte array>(Key = evtType, Value = cr.Message.Value))
            with ex ->
                logger.LogError $"Consume loop breaked: {ex}"
        }

    interface IWorker<'agg> with
        member _.Launch(ct: CancellationToken) =
            task {
                tc.Subscribe aggType
                Async.Start(work ct, ct)
                logger.LogInformation $"Subscription for {aggType} started"
            }
