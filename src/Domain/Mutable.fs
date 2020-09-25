namespace UniStream.Domain

open System
open System.Collections.Generic


module Mutable =

    type Msg<'agg> =
        | Apply of string * string * (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg) * AsyncReplyChannel<Result<'agg, string>>
        | Refresh
        | Scavenge
        | Get of string * AsyncReplyChannel<Result<'agg, string>>

    type T<'agg> =
        { Agent: MailboxProcessor<Msg<'agg>> }

    let inline agent< ^agg when ^agg : (static member Initial : ^agg)
            and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))
            and ^agg : (member Closed : bool)>
        (cfg: Config.Mutable) aggType =
        let snapshots = Dictionary<string, ^agg * uint64 * uint64>(cfg.Capacity)
        let caches =
            Dictionary<string,
                        (string -> (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg) -> AsyncReplyChannel<Result< ^agg, string>> -> unit) *
                        (AsyncReplyChannel<Result< ^agg, string>> -> unit) *
                        IDisposable>(cfg.Capacity)
        let inline shot threshold aggKey agg version = async {
            if snapshots.ContainsKey aggKey then
                let _, _, step = snapshots.[aggKey]
                if version - step > threshold then
                    let step = step + threshold
                    snapshots.[aggKey] <- (agg, version, step)
            elif version > threshold then
                let step = version / threshold * threshold
                snapshots.Add (aggKey, (agg, version, step)) }
        MailboxProcessor<Msg< ^agg>>.Start <| fun inbox ->
            let rec loop snapUsage cacheUsage = async {
                match! inbox.Receive() with
                | Apply (aggKey, traceId, apply, channel) ->
                    if caches.ContainsKey aggKey then
                        let post, _, _ = caches.[aggKey]
                        post traceId apply channel
                        return! loop snapUsage <| aggKey :: cacheUsage
                    else
                        let reader = cfg.Get aggType aggKey
                        let writer = cfg.EsFunc aggType aggKey
                        let snapshot, snapUsage =
                            if snapshots.ContainsKey aggKey then
                                let agg, version, _ = snapshots.[aggKey]
                                ValueSome (agg, version), aggKey :: snapUsage
                            else ValueNone, snapUsage
                        let shot = match cfg.Scavenge with | 0u -> None | _ -> Some <| shot cfg.Threshold aggKey
                        let post, get, agent =
                            match cfg.Batch with
                            | 0u ->
                                let agent = Basic.agent reader writer shot
                                agent.Post <| Basic.Init (traceId, apply, snapshot, channel)
                                Basic.post agent, Basic.get agent, agent :> IDisposable
                            | batch ->
                                let interval = float batch
                                let agent = Batched.agent reader writer interval shot
                                agent.Post <| Batched.Init (traceId, apply, snapshot, channel)
                                Batched.post agent, Batched.get agent, agent :> IDisposable
                        caches.Add (aggKey, (post, get, agent))
                        return! loop snapUsage <| aggKey :: cacheUsage
                | Refresh ->
                    let usage = List.distinct cacheUsage
                    if caches.Count > cfg.Capacity - 1000 then
                        let usage = List.truncate cfg.Keep usage
                        Seq.except usage caches.Keys
                        |> Seq.iter (fun id ->
                            let _, _, fty = caches.[id]
                            fty.Dispose()
                            caches.Remove id |> ignore)
                        return! loop snapUsage usage
                    else return! loop snapUsage usage
                | Scavenge ->
                    let usage = List.distinct snapUsage
                    if snapshots.Count > cfg.Capacity - 1000 then
                        let usage = List.truncate cfg.Keep usage
                        Seq.except usage snapshots.Keys
                        |> Seq.iter (snapshots.Remove >> ignore)
                        return! loop usage cacheUsage
                    else return! loop usage cacheUsage
                | Get (aggKey, channel) ->
                    if caches.ContainsKey aggKey then
                        let _, get, _ = caches.[aggKey]
                        get channel
                        return! loop snapUsage <| aggKey :: cacheUsage
                    else Error "Not found." |> Factory.reply channel |> Async.Start
                return! loop snapUsage cacheUsage
            }
            loop [] []

    let inline create (cfg: Config.Mutable) =
        let createTimer interval handler =
            let interval = float interval
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.DeclaringType.FullName
        let aggType = aggType + "-"
        let agent = agent< ^agg> cfg aggType
        Async.Start <| createTimer (cfg.Refresh * 1000u) (fun _ -> agent.Post Refresh)
        if cfg.Scavenge > 0u then Async.Start <| createTimer (cfg.Scavenge * 60u * 60u * 1000u) (fun _ -> agent.Post Scavenge)
        { Agent = agent }

    let inline apply { Agent = agent } aggKey traceId cmd = async {
        let apply = (^c : (member Apply : (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg)) cmd)
        match! agent.PostAndAsyncReply <| fun channel -> Apply (aggKey, traceId, apply, channel) with
        | Ok agg ->
            return (^agg : (member Value: ^v) agg)
        | Error err ->
            return failwithf "Execute command failed: %s" err }

    let inline get { Agent = agent } aggKey = async {
        match! agent.PostAndAsyncReply <| fun channel -> Get (aggKey, channel) with
        | Ok agg -> return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Get aggregate failed: %s" err }