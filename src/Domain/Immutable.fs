namespace UniStream.Domain

open System


module Immutable =

    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          EsFunc: Guid -> int64 -> (string * byte[])[] -> byte[] -> int64 }

    let inline create (cfg: Config.Immutable) : T< ^agg> =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let esFunc = cfg.EsFunc aggType
        { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc }

    let inline apply (t: T< ^agg>) user aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> (string * byte[])[] * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc } = t
        ld.Process user cvType aggId traceId "Start execute command."
        let init = (^agg : (static member Initial : ^agg) ())
        ld.Process user cvType aggId traceId "Initialize immutable aggregate."
        try
            let events, agg' = apply init
            ld.Process user cvType aggId traceId "Apply command success."
            try
                esFunc aggId 0L events <| MetaData.correlationId traceId |> ignore
                ld.Success user cvType aggId traceId "Save events success."
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