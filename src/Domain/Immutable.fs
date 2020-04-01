namespace UniStream.Domain

open System


module Immutable =

    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          EsFunc: EsFunc }

    let inline create (cfg: Config.Immutable) : T< ^agg> =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let esFunc = cfg.EsFunc aggType
        { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc }

    let inline apply (aggregator: T< ^agg>) user aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> byte[] -> Result<(string * byte[] * byte[]) seq * ^agg, string>)) command)
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc } = aggregator
        ld.Process user cvType aggId traceId "Start execute command."
        let init = (^agg : (static member Initial : ^agg) ())
        ld.Process user cvType aggId traceId "Initialize immutable aggregate."
        match apply init <| MetaData.correlationId traceId with
        | Ok (events, agg') ->
            ld.Process user cvType aggId traceId "Apply command success."
            try
                do! esFunc aggId 0L events |> Async.Ignore
                ld.Success user cvType aggId traceId "Execute command success."
                return (^agg : (member Value: ^v) agg')
            with ex ->
                ld.Fail user cvType aggId traceId "Save events failed."
                lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                return failwith "Save events failed."
        | Error err ->
            ld.Fail user cvType aggId traceId "Apply command failed."
            lg.Warn "Apply command failed: %s" err
            return failwith "Apply command failed."
    }