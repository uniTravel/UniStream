namespace UniStream.Domain

open System
open System.Collections.Generic


module Mutable =

    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: Guid -> int64 -> (string * byte[])[] -> byte[] -> Async<int64>
          Agent: MailboxProcessor<Accessor<'agg>> }

    // type Batch<'agg> =
    //     | Add of string * Guid * Guid * string * ('agg -> (string * byte[])[] * 'agg)
    //     | Launch of T<'agg>
    //     | Clean

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

    // let inline batch< ^agg when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> =
    //     MailboxProcessor<Batch< ^agg>>.Start <| fun inbox ->
    //         let rec loop (map: Map<Guid, Queue<string * Guid * string * (^agg -> (string * byte[])[] * ^agg)> * int64 ref>) = async {
    //             match! inbox.Receive() with
    //             | Add (user, aggId, traceId, cvType, apply) ->
    //                 if map.ContainsKey aggId then
    //                     let queue, ticks = map.[aggId]
    //                     queue.Enqueue (user, traceId, cvType, apply)
    //                     ticks := DateTime.Now.Ticks
    //                     return! loop map
    //                 else
    //                     let queue = new Queue<string * Guid * string * (^agg -> (string * byte[])[] * ^agg)>()
    //                     queue.Enqueue (user, traceId, cvType, apply)
    //                     return! loop <| map.Add (aggId, (queue, ref DateTime.Now.Ticks))
    //             | Launch { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Agent = agent; Get = get } ->
    //                 let rec launch apply aggId agg version traceId refreshed = async {
    //                     try
    //                         let events, agg' = apply agg
    //                         try
    //                             let! version = esFunc aggId (version + 1L) events <| MetaData.correlationId traceId
    //                             agent.Post <| Put (aggId, agg', version)
    //                             return None
    //                         with ex ->
    //                             lg.Error ex.StackTrace "Save events failed: %s" ex.Message
    //                             if not refreshed then
    //                                 let agg, version = Repository.sync lg aggId agg (version + 1L) get
    //                                 return! launch apply aggId agg version traceId true
    //                             else
    //                                 agent.Post <| Put (aggId, agg, version)
    //                                 return Some "Save events failed."
    //                     with ex ->
    //                         lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
    //                         agent.Post <| Put (aggId, agg, version)
    //                         return Some "Apply command failed."
    //                 }
    //                 // match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
    //                 // | Ok (agg, version) ->
    //                 //     let! result = launch apply aggId agg version traceId false
    //                 //     match result with
    //                 //     | None -> return ()
    //                 //     | Some err -> return failwith err
    //                 // | Error err ->
    //                 //     return failwith "Take aggregate failed."


    //                 return! loop map
    //             | Clean ->
    //                 return! loop map
    //         }
    //         loop Map.empty


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
        { AggType = aggType; DomainLog = ld; DiagnoseLog = lg; Get = get; EsFunc = esFunc; Agent = agent }

    let inline apply t user aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> (string * byte[])[] * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Agent = agent; Get = get } = t
        let rec launch apply aggId agg version traceId refreshed = async {
            try
                let events, agg' = apply agg
                ld.Process user cvType aggId traceId "Apply command success."
                try
                    let! version = esFunc aggId (version + 1L) events <| MetaData.correlationId traceId
                    ld.Success user cvType aggId traceId "Save events success."
                    agent.Post <| Put (aggId, agg', version)
                    return None
                with ex ->
                    lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                    if not refreshed then
                        ld.Process user cvType aggId traceId "Sync aggregate."
                        let agg, version = Repository.sync lg aggId agg (version + 1L) get
                        return! launch apply aggId agg version traceId true
                    else
                        ld.Fail user cvType aggId traceId "Save events failed."
                        agent.Post <| Put (aggId, agg, version)
                        return Some "Save events failed."
            with ex ->
                ld.Fail user cvType aggId traceId "Apply command failed."
                lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
                agent.Post <| Put (aggId, agg, version)
                return Some "Apply command failed."
        }
        ld.Process user cvType aggId traceId "Start execute command."
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            ld.Process user cvType aggId traceId "Take aggregate."
            let! result = launch apply aggId agg version traceId false
            match result with
            | None -> return ()
            | Some err -> return failwith err
        | Error err ->
            ld.Fail user cvType aggId traceId "Take aggregate failed: %s" err
            return failwith "Take aggregate failed."
    }

    // let inline batchApply (t: T< ^agg>) user aggId traceId command = async {
    //     let cvType = (^c : (static member ValueType : string) ())
    //     let apply = (^c : (member Apply: (^agg -> (string * byte[])[] * ^agg)) command)
    //     let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Agent = agent; Get = get } = t
    //     ld.Process user cvType aggId traceId "Add command to batch processor."
    //     // batch.Post <| Add (user, aggId, traceId, cvType, apply)
    // }

    let inline get { Agent = agent; Get = get } aggId = async {
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            agent.Post <| Put (aggId, agg, version)
            return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Take aggregate failed: %s" err
    }