namespace UniStream.Domain

open System


module Mutable =

    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Get
          EsFunc: EsFunc
          RepoAgent: MailboxProcessor<Repo<'agg>>
          BatAgent: MailboxProcessor<Bat<'agg>> }

    let inline repoAgent< ^agg when ^agg : (static member Initial : ^agg) and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) get repoMode blockTicks =
        let take, put, refresh, scavenge =
            match repoMode with
            | Cache r -> Repository.cTake, Repository.cPut, r * 10000000L, 2L * 60L * 60L * 10000000L
            | Snapshot (r, s, t) -> Repository.sTake, Repository.sPut t, r * 10000000L, s * 60L * 60L * 10000000L
        MailboxProcessor<Repo< ^agg>>.Start <| fun inbox ->
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

    let inline batAgent< ^agg when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> =
        MailboxProcessor<Bat< ^agg>>.Start <| fun inbox ->
            let rec loop (map: Map<Guid, (Guid * (^agg -> byte[] -> (string * byte[] * byte[]) seq * ^agg) * AsyncReplyChannel<string option>) list ref * int64 ref>) = async {
                match! inbox.Receive() with
                | Add (aggId, traceId, apply, channel) ->
                    if map.ContainsKey aggId then
                        let list, ticks = map.[aggId]
                        list := (traceId, apply, channel) :: !list
                        ticks := DateTime.Now.Ticks
                        return! loop map
                    else
                        let list = ref [ (traceId, apply, channel) ]
                        return! loop <| map.Add (aggId, (list, ref DateTime.Now.Ticks))
                | Launch ( lg, get, esFunc, agent ) ->
                    let execute agg traceId apply channel =
                        try
                            let (events, agg') = apply agg <| MetaData.correlationId traceId
                            Some events, agg', channel, None
                        with ex -> None, agg, channel, Some ex.Message
                    let rec doApply agg batch = seq {
                        match batch with
                        | (traceId, apply, channel) :: tail ->
                            let (events, agg', channel, result) = execute agg traceId apply channel
                            yield events, agg', channel, result
                            yield! doApply agg' tail
                        | [] -> ()
                    }
                    let rec buildEvents result = seq {
                        match result with
                        | (e, _, _, _) :: tail ->
                            match e with
                            | Some e -> yield! e; yield! buildEvents tail
                            | None -> yield! buildEvents tail
                        | [] -> ()
                    }
                    let rec launch aggId agg version list refreshed = async {
                        let result = !list |> List.rev |> doApply agg |> Seq.toList
                        let events = buildEvents result
                        let (_, agg', _, _) = List.last result
                        try
                            let! version = esFunc aggId (version + 1L) events
                            agent.Post <| Put (aggId, agg', version)
                            result |> List.iter (fun (_, _, channel: AsyncReplyChannel<string option>, reply) -> channel.Reply reply)
                        with ex ->
                            lg.Error ex.StackTrace "Batch apply, save events failed: %s" ex.Message
                            if not refreshed then
                                let agg, version = Repository.sync lg aggId agg (version + 1L) get
                                do! launch aggId agg version list true
                            else
                                agent.Post <| Put (aggId, agg, version)
                                result |> List.iter (fun (_, _, channel, reply) ->
                                    match reply with
                                    | None -> channel.Reply <| Some "Batch apply, save events failed."
                                    | Some err -> channel.Reply <| Some err
                                )
                    }
                    let m = map |> Map.filter (fun _ (list, _) -> not (!list).IsEmpty)
                    if not m.IsEmpty then
                        m |> Map.toSeq |> Seq.map (fun (aggId, (list, _)) -> async {
                            match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
                            | Ok (agg, version) ->
                                do! launch aggId agg version list false
                            | Error err ->
                                lg.Warn "Batch apply, take aggregate [%A] failed: %s" aggId err
                                !list |> List.iter (fun (_, _, channel) -> channel.Reply <| Some "Batch apply, take aggregate failed." )
                            list := List.empty
                        }) |> Async.Parallel |> Async.RunSynchronously |> ignore
                    return! loop map
                | Clean lg ->
                    lg.Trace "Clean batch apply task list."
                    let now = DateTime.Now.Ticks
                    let newMap = map |> Map.filter (fun _ (list, ticks) -> not (!list).IsEmpty || now - !ticks < 18000000000L )
                    return! loop newMap
            }
            loop Map.empty

    let inline create (cfg: Config.Mutable) =
        let createTimer interval handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let get = cfg.Get aggType
        let esFunc = cfg.EsFunc aggType
        let repoAgent = repoAgent< ^agg> lg get cfg.RepoMode cfg.BlockTicks
        let batAgent = batAgent< ^agg>
        match cfg.RepoMode with
        | Cache r ->
            let refresh = float r * 1000.0
            Async.Start <| createTimer refresh (fun _ -> repoAgent.Post Refresh)
        | Snapshot (r, s, _) ->
            let refresh = float r * 1000.0
            let scavenge = float s * 60.0 * 60.0 * 1000.0
            Async.Start <| createTimer refresh (fun _ -> repoAgent.Post Refresh)
            Async.Start <| createTimer scavenge (fun _ -> repoAgent.Post Scavenge)
        let aggregator = { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; Get = get; EsFunc = esFunc; RepoAgent = repoAgent; BatAgent = batAgent }
        Async.Start <| createTimer cfg.Batch (fun _ -> batAgent.Post <| Launch (lg, get, esFunc, repoAgent))
        Async.Start <| createTimer 1800000.0 (fun _ -> batAgent.Post <| Clean lg)
        aggregator

    let inline apply aggregator user aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> byte[] -> (string * byte[] * byte[]) seq * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Get = get; RepoAgent = repoAgent } = aggregator
        let rec launch apply aggId agg version traceId refreshed = async {
            try
                let (events, agg') = apply agg <| MetaData.correlationId traceId
                ld.Process user cvType aggId traceId "Apply command success."
                try
                    let! version = esFunc aggId (version + 1L) events
                    ld.Success user cvType aggId traceId "Execute command success."
                    repoAgent.Post <| Put (aggId, agg', version)
                    return Ok (^agg : (member Value: ^v) agg')
                with ex ->
                    lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                    if not refreshed then
                        ld.Process user cvType aggId traceId "Sync aggregate."
                        let agg, version = Repository.sync lg aggId agg (version + 1L) get
                        return! launch apply aggId agg version traceId true
                    else
                        ld.Fail user cvType aggId traceId "Save events failed."
                        repoAgent.Post <| Put (aggId, agg, version)
                        return Error "Save events failed."
            with ex ->
                ld.Fail user cvType aggId traceId "Apply command failed."
                lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
                repoAgent.Post <| Put (aggId, agg, version)
                return Error "Apply command failed."
        }
        ld.Process user cvType aggId traceId "Start execute command."
        match! repoAgent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            ld.Process user cvType aggId traceId "Take aggregate."
            let! result = launch apply aggId agg version traceId false
            match result with
            | Ok agg -> return agg
            | Error err -> return failwith err
        | Error err ->
            ld.Fail user cvType aggId traceId "Take aggregate failed: %s" err
            return failwith "Take aggregate failed."
    }

    let inline batchApply (aggregator: T< ^agg>) (user: string) aggId (traceId: Guid) command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> byte[] -> (string * byte[] * byte[]) seq * ^agg)) command)
        let { DomainLog = ld; BatAgent = batAgent } = aggregator
        ld.Process user cvType aggId traceId "Add command to batch processor."
        match! batAgent.PostAndAsyncReply (fun channel -> Add (aggId, traceId, apply, channel)) with
        | None -> ld.Success user cvType aggId traceId "Execute command success."
        | Some err ->
            ld.Fail user cvType aggId traceId "Execute command failed: %s" err
            return failwithf "Execute command failed: %s" err
    }

    let inline get { RepoAgent = repoAgent; Get = get } aggId = async {
        match! repoAgent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            repoAgent.Post <| Put (aggId, agg, version)
            return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Take aggregate failed: %s" err
    }