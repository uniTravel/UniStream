namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Confluent.Kafka
open Confluent.Kafka.Admin
open UniStream.Domain


[<Sealed>]
type Stream<'agg when 'agg :> Aggregate>
    (
        logger: ILogger<Stream<'agg>>,
        options: IOptionsMonitor<ConsumerConfig>,
        admin: IAdmin,
        producer: IProducer,
        consumer: IConsumer
    ) =
    let p = producer.Client
    let c = consumer.Client
    let admin = admin.Client
    let aggType = typeof<'agg>.FullName

    let init (cfg: ConsumerConfig) =
        cfg.GroupId <- cfg.GroupId + "_" + aggType
        let c = ConsumerBuilder<Byte array, byte array>(cfg).Build()
        c.Subscribe(aggType)
        c

    let consumer = init <| options.Get(Cons.Agg)
    let tp = TopicPartition(aggType, 0)

    let createTopic aggId (revision: uint64) =
        async {
            if revision = UInt64.MaxValue then
                let topic = aggType + "-" + aggId
                let cfg = Dictionary(dict [ ("retention.ms", "-1") ])

                admin
                    .CreateTopicsAsync([ TopicSpecification(Name = topic, Configs = cfg) ])
                    .Wait()
        }

    let write (aggId: Guid) (comId: Guid) (revision: uint64) (evtType: string) (evtData: byte array) =
        let aggId = aggId.ToString()
        Async.Start <| createTopic aggId revision
        let comId = comId.ToByteArray()
        let h = Headers()
        let msg = Message<byte array, byte array>(Key = comId, Value = evtData, Headers = h)
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
                  yield Encoding.ASCII.GetString cr.Message.Key, ReadOnlyMemory cr.Message.Value ]

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

        let cached =
            [ while d do
                  let cr = consumer.Consume(2000)

                  if cr.Offset = last then
                      d <- false
                  else
                      let comId = Guid cr.Message.Key
                      ch.Add(comId) |> ignore
                      yield comId ]

        logger.LogInformation($"{cached.Length} comId cached")
        cached

    interface IStream<'agg> with
        member _.Reader = read
        member _.Writer = write
        member _.Restore = restore
