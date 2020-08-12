namespace UniStream.Domain

open System
open System.Text


module Immutable =

    type T<'agg> =
        { DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Writer: string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit> }

    let inline create (cfg: Config.Immutable) : T< ^agg> =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let aggType = aggType + "-"
        let writer = cfg.EsFunc aggType
        { DomainLog = ld; DiagnoseLog = lg; Writer = writer }

    let inline apply (aggregator: T< ^agg>) user aggKey traceId command = async {
        let cvType = (^c : (static member FullName : string) ())
        let apply = (^c : (member Apply: (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; Writer = writer } = aggregator
        ld.Process user cvType aggKey traceId "Start execute command."
        let init = (^agg : (static member Initial : ^agg) ())
        ld.Process user cvType aggKey traceId "Initialize immutable aggregate."
        try
            let events, agg' = apply init
            let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
            let events = events |> Seq.map (fun (evType, data) -> evType, data, metadata)
            ld.Process user cvType aggKey traceId "Apply command success."
            try
                do! writer aggKey UInt64.MaxValue events |> Async.Ignore
                ld.Success user cvType aggKey traceId "Execute command success."
                return (^agg : (member Value: ^v) agg')
            with ex ->
                ld.Fail user cvType aggKey traceId "Save events failed."
                lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                return failwith "Save events failed."
        with ex ->
            ld.Fail user cvType aggKey traceId "Apply command failed."
            lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
            return failwith "Apply command failed."
    }