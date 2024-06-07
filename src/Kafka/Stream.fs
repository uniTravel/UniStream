namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open Microsoft.Extensions.Options
open Confluent.Kafka
open Confluent.Kafka.Admin
open UniStream.Domain


[<Sealed>]
type Stream<'agg when 'agg :> Aggregate>
    (
        options: IOptionsMonitor<ConsumerConfig>,
        admin: IAdmin,
        producer: IProducer<string, byte array>,
        consumer: IConsumer<string, byte array>
    ) =
    let p = producer.Client
    let c = consumer.Client
    let admin = admin.Client
    let aggType = typeof<'agg>.FullName
    let topic = aggType + "_Reply"

    let init (cfg: ConsumerConfig) =
        cfg.GroupId <- cfg.GroupId + "_" + aggType
        let c = ConsumerBuilder<string, byte array>(cfg).Build()
        c.Subscribe(topic)
        c

    let consumer = init <| options.Get(Cons.Agg)
    let tp = TopicPartition(topic, 0)

    let createTopic aggId (revision: uint64) =
        async {
            if revision = UInt64.MaxValue then
                let topic = aggType + "-" + aggId
                admin.CreateTopicsAsync([ TopicSpecification(Name = topic) ]).Wait()
        }

    let write (aggId: Guid) (comId: Guid) (revision: uint64) (evtType: string) (evtData: byte array) =
        let aggId = aggId.ToString()
        Async.Start <| createTopic aggId revision
        let comId = comId.ToString()
        let h = Headers()
        let msg = Message<string, byte array>(Key = comId, Value = evtData, Headers = h)
        msg.Headers.Add("aggId", Encoding.ASCII.GetBytes aggId)
        msg.Headers.Add("evtType", Encoding.ASCII.GetBytes evtType)
        p.Produce(aggType, msg)

    let read (aggId: Guid) =
        let topic = aggType + "-" + aggId.ToString()
        let tp = TopicPartition(topic, 0)
        let mutable d = true
        c.Subscribe(topic)
        c.Assign(TopicPartitionOffset(tp, Offset.Beginning))

        [ while d do
              let cr = c.Consume(2000)

              if cr.IsPartitionEOF then
                  d <- false
              else
                  yield cr.Message.Key, ReadOnlyMemory cr.Message.Value ]

    let restore (ch: HashSet<Guid>) count =
        let count = int64 count
        let mutable d = true
        let last = c.QueryWatermarkOffsets(tp, TimeSpan.FromSeconds(10)).High

        let start =
            if last.Value > count then
                Offset(last.Value - count)
            else
                Offset.Beginning

        consumer.Assign(TopicPartitionOffset(tp, start))

        [ while d do
              let cr = consumer.Consume(2000)

              if cr.Offset = last then
                  d <- false
              else
                  let comId = Guid cr.Message.Key
                  ch.Add(comId) |> ignore
                  yield comId ]

    interface IStream with
        member _.Reader = read
        member _.Writer = write
        member _.Restore = restore
