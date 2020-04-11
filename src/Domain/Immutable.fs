namespace UniStream.Domain

open System


module Immutable =

    type T<'agg> =
        { DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Writer: Writer }

    let inline create (cfg: Config.Immutable) : T< ^agg> =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let aggType = aggType + "-"
        let writer = cfg.EsFunc aggType
        { DomainLog = ld; DiagnoseLog = lg; Writer = writer }

    let inline apply (aggregator: T< ^agg>) user (aggId: Guid) (traceId: Guid) command = async {
        let aggId = aggId.ToString()
        let traceId = traceId.ToString()
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> string -> (string * byte[] * byte[]) seq * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; Writer = writer } = aggregator
        ld.Process user cvType aggId traceId "Start execute command."
        let init = (^agg : (static member Initial : ^agg) ())
        ld.Process user cvType aggId traceId "Initialize immutable aggregate."
        try
            let (events, agg') = apply init traceId
            ld.Process user cvType aggId traceId "Apply command success."
            try
                do! writer aggId 0L events |> Async.Ignore
                ld.Success user cvType aggId traceId "Execute command success."
                return (^agg : (member Value: ^v) agg')
            with ex ->
                ld.Fail user cvType aggId traceId "Save events failed."
                lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                return failwith "Save events failed."
        with ex ->
            ld.Fail user cvType aggId traceId "Apply command failed."
            lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
            return failwith "Apply command failed."
    }