namespace UniStream.Domain

open System


type AggMode = Mutable | Immutable

type RepoMode = General | Snapshot of int64


module Aggregator =

    type Accessor<'agg> =
        | Take of Guid * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of Guid * 'agg * int64
        | Refresh of int64
        | Scavenge of int64

    type StoreConfig =
        { Get: string -> Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: string -> Guid -> int64 -> (string * byte[])[] -> byte[] -> int64
          LdFunc: string -> string -> byte[] -> byte[] -> unit
          LgFunc: string -> byte[] -> unit }

    type T<'agg> =
        { AggType: string
          Timeout: int64
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: Guid -> int64 -> (string * byte[])[] -> byte[] -> int64
          Agent: MailboxProcessor<Accessor<'agg>> }

    let config get esFunc ldFunc lgFunc =
        { Get = get; EsFunc = esFunc; LdFunc = ldFunc; LgFunc = lgFunc }

    let inline agent< ^agg when ^agg : (static member Initial : ^agg) and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) get timeout repoMode =
        let take =
            match repoMode with
            | General -> Repository.gTake lg
            | Snapshot _ -> Repository.sTake lg
        let put =
            match repoMode with
            | General -> Repository.gPut lg
            | Snapshot threshold -> Repository.sPut lg threshold
        MailboxProcessor<Accessor< ^agg>>.Start <| fun inbox ->
            let rec loop repo = async {
                match! inbox.Receive() with
                | Take (aggId, channel) ->
                    try
                        let newRepo = take repo aggId channel
                        return! loop newRepo
                    with ex ->
                        channel.Reply <| Error ex.Message
                        return! loop repo
                | Put (aggId, agg, version) ->
                    try
                        let newRepo = put repo aggId agg version
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "Put aggregate failed: %s" ex.Message
                        return! loop repo
                | Refresh interval ->
                    try
                        let newRepo = Repository.refresh lg repo interval
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "Refresh aggregate cache failed: %s" ex.Message
                        return! loop repo
                | Scavenge interval ->
                    try Repository.scavenge lg interval
                    with ex -> lg.Error ex.StackTrace "Scavenge aggregate snapshot failed: %s" ex.Message
                    return! loop repo
            }
            loop <| Repository.empty lg get timeout

    let inline create cfg blockSeconds repoMode =
        let createTimer interval handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let aggMode = (^agg : (static member AggMode : AggMode)())
        if blockSeconds <= 0L || blockSeconds >= 10L then invalidArg "blockSeconds" "Block timeout must between 0~10 seconds."
        let timeout = blockSeconds * 10000000L
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let get = cfg.Get aggType
        let esFunc = cfg.EsFunc aggType
        let agent = agent< ^agg> lg get timeout repoMode
        match aggMode with
        | Immutable -> ()
        | Mutable -> Async.Start <| createTimer 15000.0 (fun _ -> agent.Post <| Refresh 150000000L)
        match repoMode with
        | General -> ()
        | Snapshot _ ->
            let t = 2.0 * 60.0 * 60.0 * 1000.0
            let interval = 2L * 60L * 60L * 10000000L
            Async.Start <| createTimer t (fun _ -> agent.Post <| Scavenge interval)
        { AggType = aggType; Timeout = timeout; DomainLog = ld; DiagnoseLog = lg; Get = get; EsFunc = esFunc; Agent = agent }

    let inline execute t aggMode apply user cvType aggId traceId = async {
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Agent = agent; Get = get } = t
        let rec launch apply aggId agg version traceId refreshed isMutable =
            try
                let events, agg' = apply agg
                ld.Process user cvType aggId traceId "Apply command success."
                try
                    let version = esFunc aggId (version + 1L) events <| MetaData.correlationId traceId
                    ld.Success user cvType aggId traceId "Save events success."
                    if isMutable then agent.Post <| Put (aggId, agg', version)
                    Ok (^agg : (member Value: ^v) agg')
                with ex ->
                    lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                    if not refreshed then
                        ld.Process user cvType aggId traceId "Sync aggregate."
                        let agg, version = Repository.sync lg aggId agg (version + 1L) t.Get
                        launch apply aggId agg version traceId true isMutable
                    else
                        ld.Fail user cvType aggId traceId "Save events failed."
                        if isMutable then agent.Post <| Put (aggId, agg, version)
                        Error "Save events failed."
            with ex ->
                ld.Fail user cvType aggId traceId "Apply command failed."
                lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
                if isMutable then agent.Post <| Put (aggId, agg, version)
                Error "Apply command failed."
        ld.Process user cvType aggId traceId "Start execute command."
        match aggMode with
        | Mutable ->
            match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
            | Ok (agg, version) ->
                ld.Process user cvType aggId traceId "Take aggregate."
                let result = launch apply aggId agg version traceId false true
                match result with
                | Ok agg -> return agg
                | Error s -> return failwith s
            | Error err ->
                ld.Fail user cvType aggId traceId "Take aggregate failed: %s" err
                return failwith "Take aggregate failed."
        | Immutable ->
            let init = (^agg : (static member Initial : ^agg) ())
            ld.Process user cvType aggId traceId "Initialize immutable aggregate."
            let result = launch apply aggId init -1L traceId true false
            match result with
            | Ok agg -> return agg
            | Error s -> return failwith s
    }

    let inline executeCommand t (user: string) aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> (string * byte[])[] * ^agg)) command)
        let aggMode = (^agg : (static member AggMode : AggMode)())
        return! execute t aggMode apply user cvType aggId traceId
    }