namespace UniStream.Domain

open System
open System.Collections.Generic


module Observer =

    type Msg<'agg> =
        | Append of string * uint64 * string * ReadOnlyMemory<byte>
        | Refresh
        | Scavenge
        | Get of string * AsyncReplyChannel<Result<'agg, string>>

    type T<'agg> =
        { DiagnoseLog: DiagnoseLog.T
          Agent: MailboxProcessor<Msg<'agg>> }

    let inline agent< ^agg when ^agg : (static member Initial : ^agg) and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))>
        (cfg: Config.Observer) (lg: DiagnoseLog.T) aggType =
        let snapshots = Dictionary<string, ^agg * uint64 * uint64>(cfg.Capacity)
        let caches =
            Dictionary<string, (uint64 -> string -> ReadOnlyMemory<byte> -> unit) * (AsyncReplyChannel<Result< ^agg, string>> -> unit) * IDisposable>(cfg.Capacity)
        let inline shot threshold aggKey agg version = async {
            if snapshots.ContainsKey aggKey then
                let _, _, step = snapshots.[aggKey]
                if version - step > threshold then
                    let step = step + threshold
                    snapshots.[aggKey] <- (agg, version, step)
            elif version > threshold then
                let step = version / threshold * threshold
                snapshots.Add (aggKey, (agg, version, step)) }
        let init aggKey snapUsage =
            let reader = cfg.Get aggType aggKey
            let snapshot, snapUsage =
                if snapshots.ContainsKey aggKey then
                    let agg, version, _ = snapshots.[aggKey]
                    ValueSome (agg, version), aggKey :: snapUsage
                else ValueNone, snapUsage
            let agg, version = Factory.raw< ^agg> lg reader snapshot
            let shot = match cfg.Scavenge with | 0u -> None | _ -> Some <| shot cfg.Threshold aggKey
            let fty =
                let agent = Observed.agent lg reader agg version shot
                Observed.append agent, Observed.get agent, agent :> IDisposable
            caches.Add (aggKey, fty)
            agg, snapUsage
        MailboxProcessor<Msg< ^agg>>.Start <| fun inbox ->
            let rec loop snapUsage cacheUsage = async {
                match! inbox.Receive() with
                | Append (aggKey, number, evType, data) ->
                    if caches.ContainsKey aggKey then
                        let post, _, _ = caches.[aggKey]
                        post number evType data
                        return! loop snapUsage <| aggKey :: cacheUsage
                    else
                        try
                            lg.Trace "Initialize cache of [%s]" aggKey
                            let _, snapUsage = init aggKey snapUsage
                            return! loop snapUsage <| aggKey :: cacheUsage
                        with ex ->
                            lg.Error ex.StackTrace "Initialize cache of [%s] failed: %s" aggKey ex.Message
                            return! loop snapUsage cacheUsage
                | Refresh ->
                    let usage = List.distinct cacheUsage
                    if caches.Count > cfg.Capacity - 1000 then
                        lg.Trace "Start refresh cache."
                        let usage = List.truncate cfg.Keep usage
                        Seq.except usage caches.Keys
                        |> Seq.iter (fun id ->
                            let _, _, fty = caches.[id]
                            fty.Dispose()
                            caches.Remove id |> ignore)
                        lg.Trace "Refresh cache finished."
                        return! loop snapUsage usage
                    else return! loop snapUsage usage
                | Scavenge ->
                    let usage = List.distinct snapUsage
                    if snapshots.Count > cfg.Capacity - 1000 then
                        lg.Trace "Start scavenge snapshot."
                        let usage = List.truncate cfg.Keep usage
                        Seq.except usage snapshots.Keys
                        |> Seq.iter (snapshots.Remove >> ignore)
                        lg.Trace "Scavenge snapshot finished."
                        return! loop usage cacheUsage
                    else return! loop usage cacheUsage
                | Get (aggKey, channel) ->
                    if caches.ContainsKey aggKey then
                        let _, get, _ = caches.[aggKey]
                        get channel
                        return! loop snapUsage <| aggKey :: cacheUsage
                    else
                        try
                            let agg, snapUsage = init aggKey snapUsage
                            Ok agg |> Factory.reply channel |> Async.Start
                            return! loop snapUsage <| aggKey :: cacheUsage
                        with ex ->
                            lg.Error ex.StackTrace "Get aggregate failed, %s" ex.Message
                            Error ex.Message |> Factory.reply channel |> Async.Start
                            return! loop snapUsage cacheUsage
                return! loop snapUsage cacheUsage
            }
            loop [] []


    let inline create (cfg: Config.Observer) =
        let createTimer interval handler =
            let interval = float interval
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let aggType = aggType + "-"
        let agent = agent< ^agg> cfg lg aggType
        Async.Start <| createTimer (cfg.Refresh * 60u * 1000u) (fun _ -> agent.Post Refresh)
        if cfg.Scavenge > 0u then Async.Start <| createTimer (cfg.Scavenge * 60u * 60u * 1000u) (fun _ -> agent.Post Scavenge)
        { DiagnoseLog = lg; Agent = agent }

    let inline append (aggregator: T< ^agg>) aggKey number evType data = async {
        aggregator.Agent.Post <| Append (aggKey, number, evType, data) }

    let inline get { Agent = agent } aggKey = async {
        match! agent.PostAndAsyncReply <| fun channel -> Get (aggKey, channel) with
        | Ok agg -> return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Get aggregate failed: %s" err }