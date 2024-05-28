namespace UniStream.Domain

open System
open System.Text
open Confluent.Kafka
open Confluent.Kafka.Admin
open UniStream.Domain


[<Sealed>]
type Stream(admin: IAdmin, producer: IProducer<string, byte array>, consumer: IConsumer<string, byte array>) =
    let p = producer.Client
    let c = consumer.Client
    let admin = admin.Client

    let createTopic aggType aggId (revision: uint64) =
        async {
            if revision = UInt64.MaxValue then
                let topic = aggType + "-" + aggId

                admin
                    .CreateTopicsAsync([ TopicSpecification(Name = topic, ReplicationFactor = 2s, NumPartitions = 1) ])
                    .Wait()
        }

    let write (aggType: string) (aggId: Guid) (revision: uint64) (evtType: string) (evtData: byte array) =
        let aggId = aggId.ToString()
        Async.Start <| createTopic aggType aggId revision
        let h = Headers()
        let msg = Message<string, byte array>(Key = aggId, Value = evtData, Headers = h)
        msg.Headers.Add("evtType", Encoding.ASCII.GetBytes evtType)
        p.Produce(aggType, msg)
        p.Flush(TimeSpan.FromSeconds(10)) |> ignore

    let read aggType (aggId: Guid) =
        let topic = aggType + "-" + aggId.ToString()
        let tp = TopicPartition(topic, Partition 0)
        let mutable d = true
        c.Subscribe(topic)
        c.Assign(TopicPartitionOffset(tp, Offset.Beginning))

        [ while d do
              let cr = c.Consume(2000)

              if cr.IsPartitionEOF then
                  d <- false
              else
                  yield cr.Message.Key, ReadOnlyMemory cr.Message.Value ]

    interface IStream with
        member _.Reader = read
        member _.Writer = write
