namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka
open UniStream.Domain


[<Sealed>]
type Stream(producerConfig: IOptionsMonitor<ProducerConfig>, consumerConfig: IOptionsMonitor<ConsumerConfig>) =
    let producer =
        ProducerBuilder<string, byte array>(producerConfig.Get("Aggregate")).Build()

    let consumer =
        ConsumerBuilder<string, byte array>(consumerConfig.Get("Aggregate")).Build()

    let write (traceId: Guid option) aggType (aggId: Guid) (revision: uint64) evtType (evtData: byte array) =
        let topic = aggType + "-" + aggId.ToString()

        producer.Produce(topic, Message<string, byte array>(Key = evtType, Value = evtData))
        producer.Flush(TimeSpan.FromSeconds(10)) |> ignore

    let read aggType (aggId: Guid) =
        let topic = aggType + "-" + aggId.ToString()
        let tp = TopicPartition(topic, Partition 0)
        let mutable c = true
        consumer.Subscribe(topic)
        consumer.Assign(TopicPartitionOffset(tp, Offset.Beginning))

        [ while c do
              let cr = consumer.Consume(2000)

              if cr.IsPartitionEOF then
                  c <- false
              else
                  yield cr.Message.Key, ReadOnlyMemory cr.Message.Value ]

    interface IStream with
        member _.Reader = read
        member _.Writer = write
