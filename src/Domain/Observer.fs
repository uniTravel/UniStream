namespace UniStream.Domain

open System


module ObServer =

    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> int64 -> (Guid * string * byte[])[] * int64
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

    let inline update t user aggId evType number data (metadata: byte[]) = async {
        let traceId = Guid.NewGuid()
        let { DomainLog = ld; DiagnoseLog = lg; Agent = agent; Get = get } = t
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

    let inline create (cfg: Config.Observer) =
        let createTimer interval handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let get = cfg.Get cfg.ObservableType
        let agent = agent< ^agg> lg get cfg.RepoMode cfg.BlockTicks
        match cfg.RepoMode with
        | Cache r ->
            let refresh = float r * 1000.0
            Async.Start <| createTimer refresh (fun _ -> agent.Post Refresh)
        | Snapshot (r, s, _) ->
            let refresh = float r * 1000.0
            let scavenge = float s * 60.0 * 60.0 * 1000.0
            Async.Start <| createTimer refresh (fun _ -> agent.Post Refresh)
            Async.Start <| createTimer scavenge (fun _ -> agent.Post Scavenge)
        let t = { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; Get = get; Agent = agent }
        cfg.SubBuilder <| update t aggType
        t

    let inline get { Agent = agent; Get = get } aggId = async {
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            agent.Post <| Put (aggId, agg, version)
            return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Take aggregate failed: %s" err
    }