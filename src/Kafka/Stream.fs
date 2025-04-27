namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open Microsoft.Extensions.Logging
open Confluent.Kafka
open Confluent.Kafka.Admin
open UniStream.Domain


[<Sealed>]
type Stream<'agg when 'agg :> Aggregate>
    (logger: ILogger<Stream<'agg>>, admin: IAdmin<'agg>, tp: IProducer<'agg>, tc: IConsumer<'agg>, ac: IConsumer<'agg>)
    =
    let admin = admin.Client
    let tp = tp.Client
    let tc = tc.Client
    let ac = ac.Client
    let aggType = typeof<'agg>.FullName

    let delivery (report: DeliveryReport<byte array, byte array>) =
        match report.Error.Code with
        | ErrorCode.NoError -> ()
        | err -> failwith <| err.GetReason()

    let write (aggId: Guid) (comId: Guid) (revision: uint64) (evtType: string) (evtData: byte array) =
        if revision = UInt64.MaxValue then
            let topic = aggType + "-" + aggId.ToString()
            let cfg = Dictionary(dict [ ("retention.ms", "-1") ])
            let spec = TopicSpecification(Name = topic, NumPartitions = 1, Configs = cfg)
            admin.CreateTopicsAsync [ spec ] |> ignore

        try
            let aId = aggId.ToByteArray()
            let h = Headers()
            let msg = Message<byte array, byte array>(Key = aId, Value = evtData, Headers = h)
            msg.Headers.Add("comId", comId.ToByteArray())
            msg.Headers.Add("evtType", Encoding.ASCII.GetBytes evtType)
            tp.Produce(aggType, msg, delivery)
        with ex ->
            raise <| WriteException($"Write {evtType} failed", ex)

    let read (aggId: Guid) =
        let rec get events =
            match ac.Consume 10000 with
            | cr when cr.IsPartitionEOF -> events
            | cr ->
                let m = Encoding.ASCII.GetString cr.Message.Key, ReadOnlyMemory cr.Message.Value
                get <| m :: events

        try
            ac.Assign(TopicPartition($"{aggType}-{aggId}", 0))
            get []
        with ex ->
            raise <| ReadException($"Read strem of {aggType}[{aggId}] failed", ex)

    let restore (ch: HashSet<Guid>) (latest: int) =
        let targetTime = DateTime.UtcNow.AddMinutes -latest
        let timestamp = Timestamp(targetTime, TimestampType.CreateTime)
        let eofStatus = Dictionary<TopicPartition, bool>()

        let tpts =
            admin.GetMetadata(TimeSpan.FromSeconds 10.0).Topics.Find(fun t -> t.Topic = aggType).Partitions
            |> Seq.map (fun pm -> TopicPartitionTimestamp(TopicPartition(aggType, pm.PartitionId), timestamp))

        let tpos =
            tc.OffsetsForTimes(tpts, TimeSpan.FromSeconds 10.0)
            |> Seq.filter (fun tpo -> tpo.Offset <> Offset.End)

        tpos |> Seq.iter (fun tpo -> eofStatus[tpo.TopicPartition] <- false)

        let watermarks =
            eofStatus.Keys
            |> Seq.map (fun x -> x, tc.QueryWatermarkOffsets(x, TimeSpan.FromSeconds 10.0).High)
            |> dict

        tc.Assign tpos

        while eofStatus.Values |> Seq.contains false do
            let cr = tc.Consume 10000
            let comId = Guid(cr.Message.Headers.GetLastBytes "comId")
            ch.Add comId |> ignore

            if cr.Offset.Value = watermarks[cr.TopicPartition].Value - 1L then
                eofStatus[cr.TopicPartition] <- true

        let cached = List.ofSeq ch
        logger.LogInformation $"{cached.Length} comId of {aggType} cached"
        tc.Unassign()
        tc.Close()
        cached

    interface IStream<'agg> with
        member _.Reader = read
        member _.Writer = write
        member _.Restore = restore
