namespace UniStream.Domain

open System
open System.Collections.Generic


module Mutable =

    type Repo<'agg> =
        | Take of string * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of string * 'agg * int64
        | Refresh
        | Scavenge

    type Bat<'agg> =
        | Add of string * string * ('agg -> string -> (string * byte[] * byte[]) seq * 'agg) * AsyncReplyChannel<string voption>
        | Launch of DiagnoseLog.Logger * Reader * Writer * MailboxProcessor<Repo<'agg>>

    type T<'agg> =
        { DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Reader: Reader
          Writer: Writer
          RepoAgent: MailboxProcessor<Repo<'agg>>
          BatAgent: MailboxProcessor<Bat<'agg>> }

    let inline repoAgent< ^agg when ^agg : (static member Initial : ^agg) and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) reader repoMode =
        let take, put, capacity =
            match repoMode with
            | Cache (c, _) -> Repository.cTake, Repository.cPut, c
            | Snapshot (c, _, _, t) -> Repository.sTake, Repository.sPut t, c
        MailboxProcessor<Repo< ^agg>>.Start <| fun inbox ->
            let rec loop repo = async {
                match! inbox.Receive() with
                | Take (aggId, channel) ->
                    try
                        return! loop <| take repo aggId channel
                    with ex ->
                        lg.Error ex.StackTrace "Take aggregate [%s] failed: %s" aggId ex.Message
                        channel.Reply <| Error ex.Message
                        return! loop repo
                | Put (aggId, agg, version) ->
                    try
                        return! loop <| put repo aggId agg version
                    with ex ->
                        lg.Error ex.StackTrace "Put aggregate failed: %s" ex.Message
                        return! loop repo
                | Refresh ->
                    try
                        return! loop <| Repository.refresh repo
                    with ex ->
                        lg.Error ex.StackTrace "Refresh aggregate cache failed: %s" ex.Message
                        return! loop repo
                | Scavenge ->
                    try
                        return! loop <| Repository.scavenge repo
                    with ex ->
                        lg.Error ex.StackTrace "Scavenge aggregate snapshot failed: %s" ex.Message
                        return! loop repo
            }
            loop <| Repository.empty lg reader capacity

    let inline batAgent< ^agg when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> =
        MailboxProcessor<Bat< ^agg>>.Start <| fun inbox ->
            let rec loop (dic: Dictionary<string, (string * (^agg -> string -> (string * byte[] * byte[]) seq * ^agg) * AsyncReplyChannel<string voption>) list ref>) = async {
                match! inbox.Receive() with
                | Add (aggId, traceId, apply, channel) ->
                    if dic.ContainsKey aggId then
                        let batch = dic.[aggId]
                        batch := (traceId, apply, channel) :: !batch
                    else
                        dic.Add (aggId, ref [ (traceId, apply, channel) ])
                | Launch (lg, get, esFunc, agent) ->
                    let execute agg traceId apply channel =
                        try
                            let events, agg' = apply agg traceId
                            events, agg', channel, ValueNone
                        with ex -> Seq.empty, agg, channel, ValueSome ex.Message
                    let rec doApply agg batch = seq {
                        match batch with
                        | (traceId, apply, channel) :: tail ->
                            let events, agg', channel, result = execute agg traceId apply channel
                            yield events, agg', channel, result
                            yield! doApply agg' tail
                        | [] -> ()}
                    let rec launch aggId agg version batch refreshed = async {
                        let result = !batch |> List.rev |> doApply agg
                        let events = result |> Seq.collect (fun (e, _, _, _) -> e)
                        try
                            let! version = esFunc aggId (version + 1L) events
                            let _, agg', _, _ = Seq.last result
                            agent.Post <| Put (aggId, agg', version)
                            result |> Seq.iter (fun (_, _, channel: AsyncReplyChannel<string voption>, reply) -> channel.Reply reply)
                        with ex ->
                            lg.Error ex.StackTrace "Batch apply, save events failed: %s" ex.Message
                            if not refreshed then
                                try
                                    let agg, version = Repository.sync lg aggId agg (version + 1L) get
                                    do! launch aggId agg version batch true
                                with _ ->
                                    agent.Post <| Put (aggId, agg, version)
                                    result |> Seq.iter (fun (_, _, channel, reply) ->
                                        match reply with
                                        | ValueNone -> channel.Reply <| ValueSome "Sync aggregate failed."
                                        | ValueSome err -> channel.Reply <| ValueSome err)
                            else
                                agent.Post <| Put (aggId, agg, version)
                                result |> Seq.iter (fun (_, _, channel, reply) ->
                                    match reply with
                                    | ValueNone -> channel.Reply <| ValueSome "Save events failed."
                                    | ValueSome err -> channel.Reply <| ValueSome err)
                    }
                    if dic.Count > 0 then
                        dic.Keys
                        |> Seq.map (fun aggId -> async {
                            let batch = dic.[aggId]
                            match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
                            | Ok (agg, version) ->
                                do! launch aggId agg version batch false
                            | Error _ ->
                                !batch |> List.iter (fun (_, _, channel) -> channel.Reply <| ValueSome "Take aggregate failed.")
                        }) |> Async.Parallel |> Async.RunSynchronously |> ignore
                        dic.Clear()
                return! loop dic
            }
            loop <| Dictionary<string, (string * (^agg -> string -> (string * byte[] * byte[]) seq * ^agg) * AsyncReplyChannel<string voption>) list ref>(10000)

    let inline create (cfg: Config.Mutable) =
        let createTimer interval handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let aggType = aggType + "-"
        let reader = cfg.Get aggType
        let writer = cfg.EsFunc aggType
        let repoAgent = repoAgent< ^agg> lg reader cfg.RepoMode
        let batAgent = batAgent< ^agg>
        match cfg.RepoMode with
        | Cache (_, r) ->
            let ri = float r * 1000.0
            Async.Start <| createTimer ri (fun _ -> repoAgent.Post Refresh)
        | Snapshot (_, r, s, _) ->
            let ri = float r * 1000.0
            let si = float s * 60.0 * 60.0 * 1000.0
            Async.Start <| createTimer ri (fun _ -> repoAgent.Post Refresh)
            Async.Start <| createTimer si (fun _ -> repoAgent.Post Scavenge)
        let aggregator = { DomainLog = ld; DiagnoseLog = lg; Reader = reader; Writer = writer; RepoAgent = repoAgent; BatAgent = batAgent }
        Async.Start <| createTimer cfg.Batch (fun _ -> batAgent.Post <| Launch (lg, reader, writer, repoAgent))
        aggregator

    let inline apply aggregator user (aggId: Guid) (traceId: Guid) command = async {
        let aggId = aggId.ToString()
        let traceId = traceId.ToString()
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> string -> (string * byte[] * byte[]) seq * ^agg)) command)
        let { DomainLog = ld; DiagnoseLog = lg; Reader = reader; Writer = writer; RepoAgent = repoAgent } = aggregator
        let rec launch apply aggId agg version traceId refreshed = async {
            try
                let (events, agg') = apply agg traceId
                ld.Process user cvType aggId traceId "Apply command success."
                try
                    let! version = writer aggId (version + 1L) events
                    ld.Success user cvType aggId traceId "Execute command success."
                    repoAgent.Post <| Put (aggId, agg', version)
                    return Ok (^agg : (member Value: ^v) agg')
                with ex ->
                    lg.Error ex.StackTrace "Save events failed: %s" ex.Message
                    if not refreshed then
                        ld.Process user cvType aggId traceId "Sync aggregate."
                        try
                            let agg, version = Repository.sync lg aggId agg (version + 1L) reader
                            return! launch apply aggId agg version traceId true
                        with _ ->
                            ld.Fail user cvType aggId traceId "Sync aggregate failed."
                            repoAgent.Post <| Put (aggId, agg, version)
                            return Error "Sync aggregate failed."
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

    let inline batchApply (aggregator: T< ^agg>) (user: string) (aggId: Guid) (traceId: Guid) command = async {
        let aggId = aggId.ToString()
        let traceId = traceId.ToString()
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> string -> (string * byte[] * byte[]) seq * ^agg)) command)
        let { DomainLog = ld; BatAgent = batAgent } = aggregator
        ld.Process user cvType aggId traceId "Add command to batch processor."
        match! batAgent.PostAndAsyncReply (fun channel -> Add (aggId, traceId, apply, channel)) with
        | ValueNone -> ld.Success user cvType aggId traceId "Execute command success."
        | ValueSome err ->
            ld.Fail user cvType aggId traceId "Execute command failed: %s" err
            return failwithf "Execute command failed: %s" err
    }

    let inline get { RepoAgent = repoAgent } aggId = async {
        match! repoAgent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            repoAgent.Post <| Put (aggId, agg, version)
            return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Take aggregate failed: %s" err
    }