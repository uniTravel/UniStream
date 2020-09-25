namespace UniStream.Domain

open System
open System.Text


module Immutable =

    type T<'agg> =
        { Writer: string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit> }

    let inline create (cfg: Config.Immutable) : T< ^agg> =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let aggType = aggType + "-"
        let writer = cfg.EsFunc aggType
        { Writer = writer }

    let inline apply (aggregator: T< ^agg>) aggKey traceId cmd = async {
        let apply = (^c : (member Apply : (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg)) cmd)
        let { Writer = writer } = aggregator
        let init = (^agg : (static member Initial : ^agg) ())
        let events, agg' = apply init
        let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
        let events = events |> Seq.map (fun (evType, data) -> evType, data, metadata)
        do! writer aggKey UInt64.MaxValue events |> Async.Ignore
        return (^agg : (member Value: ^v) agg') }