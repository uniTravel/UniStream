namespace UniStream.Domain

open System
open System.Text


module Immutable =

    type T<'agg> =
        { DomainLog: DomainLog.T
          DiagnoseLog: DiagnoseLog.T
          Writer: string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit> }

    let inline create (cfg: Config.Immutable) : T< ^agg> =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let aggType = aggType + "-"
        let writer = cfg.EsFunc aggType
        { DomainLog = ld; DiagnoseLog = lg; Writer = writer }

    let inline apply (aggregator: T< ^agg>) user aggKey cvType traceId data = async {
        let { DomainLog = ld; DiagnoseLog = lg; Writer = writer } = aggregator
        ld.Process user cvType aggKey traceId "Start execute command."
        let init = (^agg : (static member Initial : ^agg) ())
        let applyCommand = (^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg)) init)
        ld.Process user cvType aggKey traceId "Initialize immutable aggregate."
        try
            lg.Trace "Apply command to immutable aggregate and write to stream."
            let events, agg' = applyCommand cvType data
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
            return failwith "Apply command failed." }