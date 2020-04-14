namespace UniStream.Domain

open System
open System.Timers


module Observer =

    type Repo<'agg> =
        | Take of string * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of string * 'agg * int64
        | Refresh of AsyncReplyChannel<Map<string, unit>>
        | Scavenge

    type Sub =
        | Subscribe of string * SubDropHandler * AsyncReplyChannel<string voption>
        | Unsubscribe of string
        | Clean of Map<string, unit>

    type T<'agg> =
        { DiagnoseLog: DiagnoseLog.Logger
          SubAgent: MailboxProcessor<Sub>
          RepoAgent: MailboxProcessor<Repo<'agg>> }

    let inline repoAgent< ^agg when ^agg : (static member Initial : ^agg) and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) (cfg: Config.Observer) =
        let take, put, refresh, scavenge =
            match cfg.RepoMode with
            | Cache r -> Repository.cTake, Repository.cPut, r * 10000000L, 2L * 60L * 60L * 10000000L
            | Snapshot (r, s, t) -> Repository.sTake, Repository.sPut t, r * 10000000L, s * 60L * 60L * 10000000L
        MailboxProcessor<Repo< ^agg>>.Start <| fun inbox ->
            let rec loop repo = async {
                match! inbox.Receive() with
                | Take (id, channel) ->
                    try
                        let newRepo = take repo id channel
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "Take aggregate [%A] failed: %s" id ex.Message
                        channel.Reply <| Error ex.Message
                        return! loop repo
                | Put (id, agg, version) ->
                    try
                        let newRepo = put repo id agg version
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "Put aggregate failed: %s" ex.Message
                        return! loop repo
                | Refresh channel ->
                    try
                        let newRepo = Repository.refresh repo refresh
                        let ids = newRepo.Cache |> Map.map (fun _ _ -> ())
                        channel.Reply ids
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
            loop <| Repository.empty lg cfg.Reader cfg.BlockTicks

    let inline subAgent (lg: DiagnoseLog.Logger) (cfg: Config.Observer) (repoAgent: MailboxProcessor<Repo< ^agg>>) handler =
        MailboxProcessor<Sub>.Start <| fun inbox ->
            let rec loop (subs: Map<string, unit -> unit>) = async {
                match! inbox.Receive() with
                | Subscribe (id, dropHandler, channel) ->
                    if subs.ContainsKey id then
                        channel.Reply ValueNone
                        return! loop subs
                    else
                        let unsub = cfg.SubBuilder id handler dropHandler
                        let newSubs = subs.Add (id, unsub)
                        match! repoAgent.PostAndAsyncReply (fun channel -> Take (id, channel)) with
                        | Ok (agg, version) ->
                            let agg', version = Repository.sync lg id agg (version + 1L) cfg.Reader
                            repoAgent.Post <| Put (id, agg', version)
                            channel.Reply ValueNone
                        | Error err -> channel.Reply <| ValueSome err
                        return! loop newSubs
                | Unsubscribe id ->
                    return! loop <| subs.Remove id
                | Clean ids ->
                    let newSubs =
                        subs |> Map.filter (fun key unsub ->
                            if ids.ContainsKey key then true
                            else unsub(); false
                        )
                    return! loop newSubs
            }
            loop Map.empty

    let inline update (repoAgent: MailboxProcessor<Repo< ^agg>>) lg reader id evType number data (metadata: byte[]) = async {
        match! repoAgent.PostAndAsyncReply (fun channel -> Take (id, channel)) with
        | Ok (agg, version) ->
            try
                let v = version + 1L
                match number with
                | n when n = v ->
                    let agg' = (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType data
                    repoAgent.Post <| Put (id, agg', n)
                | n when n = version ->
                    repoAgent.Post <| Put (id, agg, version)
                | n when n > v ->
                    let agg, version = Repository.sync lg id agg v reader
                    repoAgent.Post <| Put (id, agg, version)
                | n -> failwithf "Unkown error, version is %d but number is %d." version n
            with ex ->
                lg.Error ex.StackTrace "Update observer failed: %s" ex.Message
                repoAgent.Post <| Put (id, agg, version)
        | Error err ->
            lg.Warn "Take aggregate failed: %s" err
    }

    let inline subDropped (subAgent: MailboxProcessor<Sub>) (lg: DiagnoseLog.Logger) id reason (ex: exn) = async {
        lg.Error ex.StackTrace "Subcribe dropped because of %s." reason
        subAgent.Post <| Unsubscribe id
    }

    let inline create (cfg: Config.Observer) =
        let createTimer interval handler =
            let timer = new Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let repoAgent = repoAgent< ^agg> lg cfg
        let subAgent = subAgent lg cfg repoAgent <| update repoAgent lg cfg.Reader
        let refresh (e: ElapsedEventArgs) =
            let ids = repoAgent.PostAndAsyncReply Refresh |> Async.RunSynchronously
            subAgent.Post <| Clean ids
        match cfg.RepoMode with
        | Cache r ->
            let ri = float r * 1000.0
            Async.Start <| createTimer ri refresh
        | Snapshot (r, s, _) ->
            let ri = float r * 60.0 * 1000.0
            let si = float s * 60.0 * 60.0 * 1000.0
            Async.Start <| createTimer ri refresh
            Async.Start <| createTimer si (fun _ -> repoAgent.Post Scavenge)
        { DiagnoseLog = lg; SubAgent = subAgent; RepoAgent = repoAgent }

    let inline get (aggregator: T< ^agg>) id = async {
        let { DiagnoseLog = lg; SubAgent = subAgent; RepoAgent = repoAgent } = aggregator
        match subAgent.PostAndReply <| fun channel -> Subscribe (id, subDropped subAgent lg id, channel) with
        | ValueNone -> ()
        | ValueSome err -> return failwithf "Check subscribe failed: %s" err
        match! repoAgent.PostAndAsyncReply (fun channel -> Take (id, channel)) with
        | Ok (agg, version) ->
            repoAgent.Post <| Put (id, agg, version)
            return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Take aggregate failed: %s" err
    }