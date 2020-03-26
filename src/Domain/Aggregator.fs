namespace UniStream.Domain

open System


module Aggregator =

    type Immutable<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          SaveI: Guid -> int64 -> (string * byte[])[] -> byte[] -> int64 }

    type Mutable<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          GetM: Guid -> int64 -> (Guid * string * byte[])[] * int64
          SaveM: Guid -> int64 -> (string * byte[])[] -> byte[] -> int64
          Agent: MailboxProcessor<Accessor<'agg>> }

    type Observer<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          GetO: Guid -> int64 -> (Guid * string * byte[])[] * int64
          Agent: MailboxProcessor<Accessor<'agg>> }

    let inline agent< ^agg when ^agg : (static member Initial : ^agg) and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) get repoMode blockTicks =
        let take, put, refresh, scavenge =
            match repoMode with
            | Cache r -> Repository.cTake, Repository.cPut, r * 10000000L, 2L * 60L * 60L * 10000000L
            | Snapshot (r, s, t) -> Repository.sTake, Repository.sPut t, r * 10000000L, s * 60L * 60L * 10000000L
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
                | Refresh ->
                    try
                        let newRepo = Repository.refresh repo refresh
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "Refresh aggregate cache failed: %s" ex.Message
                        return! loop repo
                | Scavenge ->
                    try
                        let newRepo = Repository.scavenge repo scavenge
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "Scavenge aggregate snapshot failed: %s" ex.Message
                        return! loop repo
            }
            loop <| Repository.empty lg get blockTicks

    let inline immutableCommand (t: Immutable< ^agg>) user aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> (string * byte[])[] * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; SaveI = esFunc } = t
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

    let inline mutableCommand t user aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> (string * byte[])[] * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; SaveM = esFunc; Agent = agent; GetM = get } = t
        let rec launch apply aggId agg version traceId refreshed =
            try
                let events, agg' = apply agg
                ld.Process user cvType aggId traceId "Apply command success."
                try
                    let version = esFunc aggId (version + 1L) events <| MetaData.correlationId traceId
                    ld.Success user cvType aggId traceId "Save events success."
                    agent.Post <| Put (aggId, agg', version)
                    Ok (^agg : (member Value: ^v) agg')
                with ex ->
                    lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                    if not refreshed then
                        ld.Process user cvType aggId traceId "Sync aggregate."
                        let agg, version = Repository.sync lg aggId agg (version + 1L) get
                        launch apply aggId agg version traceId true
                    else
                        ld.Fail user cvType aggId traceId "Save events failed."
                        agent.Post <| Put (aggId, agg, version)
                        Error "Save events failed."
            with ex ->
                ld.Fail user cvType aggId traceId "Apply command failed."
                lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
                agent.Post <| Put (aggId, agg, version)
                Error "Apply command failed."
        ld.Process user cvType aggId traceId "Start execute command."
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            ld.Process user cvType aggId traceId "Take aggregate."
            let result = launch apply aggId agg version traceId false
            match result with
            | Ok agg -> return agg
            | Error s -> return failwith s
        | Error err ->
            ld.Fail user cvType aggId traceId "Take aggregate failed: %s" err
            return failwith "Take aggregate failed."
    }

    let inline update t user aggId evType number data (metadata: byte[]) = async {
        let traceId = Guid.NewGuid()
        let { DomainLog = ld; DiagnoseLog = lg; Agent = agent; GetO = get } = t
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            ld.Process user evType aggId traceId "Take aggregate."
            try
                let v = version + 1L
                match number with
                | n when n = v ->
                    let agg' = (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType data
                    agent.Post <| Put (aggId, agg', n)
                | n when n = version ->
                    agent.Post <| Put (aggId, agg, version)
                | n when n > v ->
                    let agg, version = Repository.sync lg aggId agg v get
                    agent.Post <| Put (aggId, agg, version)
                | n -> failwithf "Unkown error, version is %d but number is %d." version n
            with ex ->
                ld.Fail user evType aggId traceId "Update observer failed."
                lg.Error ex.StackTrace "Update observer failed: %s" ex.Message
                agent.Post <| Put (aggId, agg, version)
        | Error err ->
            ld.Fail user evType aggId traceId "Take aggregate failed: %s" err
            lg.Warn "Take aggregate failed: %s" err
    }

    let inline get t aggId = async {
        let { Agent = agent; GetO = get } = t
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            agent.Post <| Put (aggId, agg, version)
            return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Take aggregate failed: %s" err
    }

    let inline createImmutable (cfg: Config.Immutable) : Immutable< ^agg> =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let esFunc = cfg.EsFunc aggType
        { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; SaveI = esFunc }

    let inline start repoMode (agent: MailboxProcessor<Accessor< ^agg>>) =
        let createTimer interval handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        match repoMode with
        | Cache r ->
            let refresh = float r * 1000.0
            Async.Start <| createTimer refresh (fun _ -> agent.Post Refresh)
        | Snapshot (r, s, _) ->
            let refresh = float r * 1000.0
            let scavenge = float s * 60.0 * 60.0 * 1000.0
            Async.Start <| createTimer refresh (fun _ -> agent.Post Refresh)
            Async.Start <| createTimer scavenge (fun _ -> agent.Post Scavenge)

    let inline createMutable (cfg: Config.Mutable) =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let get = cfg.Get aggType
        let esFunc = cfg.EsFunc aggType
        let agent = agent< ^agg> lg get cfg.RepoMode cfg.BlockTicks
        start cfg.RepoMode agent
        { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; GetM = get; SaveM = esFunc; Agent = agent }

    let inline createObserver (cfg: Config.Observer) =
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let get = cfg.Get cfg.ObservableType
        let agent = agent< ^agg> lg get cfg.RepoMode cfg.BlockTicks
        start cfg.RepoMode agent
        let t = { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; GetO = get; Agent = agent }
        cfg.SubBuilder <| update t aggType
        t