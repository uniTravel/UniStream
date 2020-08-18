namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text


module Mutable =

    type Msg<'agg> =
        | Apply of string * string * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Refresh
        | Scavenge
        | Get of string * AsyncReplyChannel<Result<'agg, string>>

    type T<'agg> =
        { DomainLog: DomainLog.T
          DiagnoseLog: DiagnoseLog.T
          Agent: MailboxProcessor<Msg<'agg>> }

    let inline agent< ^agg when ^agg : (static member Initial : ^agg)
            and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))
            and ^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg))
            and ^agg : (member Closed : bool)>
        (cfg: Config.Mutable) (lg: DiagnoseLog.T) aggType =
        let snapshots = Dictionary<string, ^agg * uint64 * uint64>(cfg.Capacity)
        let caches =
            Dictionary<string,
                        (string -> string -> ReadOnlyMemory<byte> -> AsyncReplyChannel<Result< ^agg, string>> -> unit) *
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
                | Apply (aggKey, cvType, traceId, data, channel) ->
                    if caches.ContainsKey aggKey then
                        let post, _, _ = caches.[aggKey]
                        post cvType traceId data channel
                        return! loop snapUsage <| aggKey :: cacheUsage
                    else
                        try
                            lg.Trace "Initialize cache of [%s]" aggKey
                            let reader = cfg.Get aggType aggKey
                            let writer = cfg.EsFunc aggType aggKey
                            let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                            let snapshot, snapUsage =
                                if snapshots.ContainsKey aggKey then
                                    let agg, version, _ = snapshots.[aggKey]
                                    ValueSome (agg, version), aggKey :: snapUsage
                                else ValueNone, snapUsage
                            let agg, version = Factory.init< ^agg> lg reader writer snapshot (cvType, data, metadata, channel)
                            let shot = match cfg.Scavenge with | 0u -> None | _ -> Some <| shot cfg.Threshold aggKey
                            let fty =
                                match cfg.Batch with
                                | 0u ->
                                    let agent = Basic.agent lg reader writer agg version shot
                                    Basic.post agent, Basic.get agent, agent :> IDisposable
                                | batch ->
                                    let interval = float batch
                                    let agent = Batched.agent lg reader writer interval agg version shot
                                    Batched.post agent, Batched.get agent, agent :> IDisposable
                            caches.Add (aggKey, fty)
                            return! loop snapUsage <| aggKey :: cacheUsage
                        with ex ->
                            lg.Error ex.StackTrace "Initialize cache of [%s] failed: %s" aggKey ex.Message
                            Error ex.Message |> Factory.reply channel |> Async.Start
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
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let aggType = aggType + "-"
        let agent = agent< ^agg> cfg lg aggType
        Async.Start <| createTimer (cfg.Refresh * 1000u) (fun _ -> agent.Post Refresh)
        if cfg.Scavenge > 0u then Async.Start <| createTimer (cfg.Scavenge * 60u * 60u * 1000u) (fun _ -> agent.Post Scavenge)
        { DomainLog = ld; DiagnoseLog = lg; Agent = agent }

    let inline apply { DomainLog = ld; Agent = agent } user aggKey cvType traceId data = async {
        ld.Process user cvType aggKey traceId "Start execute command."
        match! agent.PostAndAsyncReply <| fun channel -> Apply (aggKey, cvType, traceId, data, channel) with
        | Ok agg ->
            ld.Success user cvType aggKey traceId "Execute command success."
            return (^agg : (member Value: ^v) agg)
        | Error err ->
            ld.Fail user cvType aggKey traceId "Execute command failed: %s" err
            return failwithf "Execute command failed: %s" err }

    let inline get { Agent = agent } aggKey = async {
        match! agent.PostAndAsyncReply <| fun channel -> Get (aggKey, channel) with
        | Ok agg -> return (^agg : (member Value: ^v) agg)
        | Error err -> return failwithf "Get aggregate failed: %s" err }