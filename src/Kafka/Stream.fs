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
        tp: IProducer,
        tc: IConsumer
    ) =
    let tp = tp.Client
    let tc = tc.Client
    let admin = admin.Client
    let aggType = typeof<'agg>.FullName
    let cfg = options.Get(Cons.Agg)

    let ac =
        cfg.GroupId <- cfg.GroupId + aggType
        ConsumerBuilder<Byte array, byte array>(cfg).Build()

    let createTopic (aggId: Guid) (revision: uint64) =
        async {
            if revision = UInt64.MaxValue then
                let topic = aggType + "-" + aggId.ToString()
                let cfg = Dictionary(dict [ ("retention.ms", "-1") ])

                admin
                    .CreateTopicsAsync([ TopicSpecification(Name = topic, NumPartitions = 1, Configs = cfg) ])
                    .Wait()
        }

    let write (aggId: Guid) (comId: Guid) (revision: uint64) (evtType: string) (evtData: byte array) =
        try
            Async.Start <| createTopic aggId revision
            let aggId = aggId.ToByteArray()
            let h = Headers()
            let msg = Message<byte array, byte array>(Key = aggId, Value = evtData, Headers = h)
            msg.Headers.Add("comId", comId.ToByteArray())
            msg.Headers.Add("evtType", Encoding.ASCII.GetBytes evtType)
            tp.Produce(aggType, msg)
        with ex ->
            logger.LogError($"Write {evtType} of {aggId} error: {ex.Message}")
            raise <| WriteException($"Write {evtType} of {aggId} error", ex)

    let read (aggId: Guid) =
        try
            let aggId = aggId.ToString()
            let topic = aggType + "-" + aggId
            let mutable d = true
            ac.Assign(TopicPartitionOffset(topic, 0, Offset.Beginning))

            let result =
                [ while d do
                      let cr = ac.Consume(2000)

                      if cr.IsPartitionEOF then
                          d <- false
                      else
                          yield Encoding.ASCII.GetString cr.Message.Key, ReadOnlyMemory cr.Message.Value ]

            result
        with ex ->
            logger.LogError($"Read strem of {aggId} error: {ex.Message}")
            raise <| ReadException($"Read strem of {aggId} error", ex)

    let restore (ch: HashSet<Guid>) count =
        let mutable d = true

        admin
            .GetMetadata(TimeSpan.FromSeconds 2.0)
            .Topics.Find(fun t -> t.Topic = aggType)
            .Partitions
        |> Seq.map (fun x -> TopicPartition(aggType, x.PartitionId))
        |> tc.Assign

        let result =
            [ while d do
                  let cr = tc.Consume(2000)

                  if cr = null then
                      d <- false
                  else
                      let comId = Guid(cr.Message.Headers.GetLastBytes("comId"))
                      ch.Add(comId) |> ignore
                      yield comId ]

        let cached = result |> List.skip (result.Length - count)
        logger.LogInformation($"{cached.Length} comId of {aggType} cached")
        cached

    interface IStream<'agg> with
        member _.Reader = read
        member _.Writer = write
        member _.Restore = restore
