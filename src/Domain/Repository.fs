namespace UniStream.Domain

open System
open System.Collections.Generic


module Repository =

    type State<'agg> =
        | Available of 'agg * int64 * int64
        | Empty
        | Pending of int64
        | Blocked of int64

    type T<'agg> =
        { Get: string -> int64 -> (Guid * string * byte[])[] * int64
          Timeout: int64
          Cache: Map<string, Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref> }

    let snapshot<'agg> : Map<string, 'agg * int64 * int64 * int64 ref> ref = ref Map.empty

    let empty (lg: DiagnoseLog.Logger) get timeout =
        lg.Trace "Initialize aggregate repository."
        { Get = get; Timeout = timeout; Cache = Map.empty }

    let inline sync (lg: DiagnoseLog.Logger) aggId agg version (get: string -> int64 -> (Guid * string * byte[])[] * int64) =
        lg.Trace "Begin sync aggregate %s, start version is %d." aggId version
        let (events, version) = get aggId version
        lg.Trace "Get %d data, current version is %d." events.Length version
        match events with
        | [||] -> agg, version
        | _ ->
            let agg =
                Array.fold (fun agg elem ->
                    let (_, evType, evBytes) = elem
                    (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType evBytes
                ) agg events
            agg, version

    let fromCache (lg: DiagnoseLog.Logger) (repo: T<'agg>) aggId (channel: AsyncReplyChannel<Result<'agg * int64, string>>) =
        let (queue, state) = repo.Cache.[aggId]
        lg.Trace "Get aggregate [%s] from cache." aggId
        match !state with
        | Available (agg, version, _) ->
            state := Empty
            channel.Reply <| Ok (agg, version)
        | Empty ->
            let now = DateTime.Now.Ticks
            queue.Enqueue (now, channel)
            state := Pending now
        | Pending ticks ->
            let now = DateTime.Now.Ticks
            queue.Enqueue (now, channel)
            if now - ticks > repo.Timeout then state := Blocked now
        | Blocked _ -> failwithf "Status of aggregate [%s] is 'Blocked'." aggId
        repo

    let inline fromStore (lg: DiagnoseLog.Logger) repo aggId agg version (channel: AsyncReplyChannel<Result< ^agg * int64, string>>) =
        lg.Trace "Begin get aggregate [%s] from stream store, start version is %d." aggId version
        let (events, version) = repo.Get aggId version
        lg.Trace "Get %d data, current version is %d." events.Length version
        let cache = repo.Cache.Add (aggId, (new Queue<int64 * AsyncReplyChannel<Result< ^agg * int64, string>>>(), ref Empty))
        match events with
        | [||] -> channel.Reply <| Ok (agg, version - 1L)
        | _ ->
            let agg =
                Array.fold (fun agg elem ->
                    let (_, evType, evBytes) = elem
                    (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType evBytes
                ) agg events
            channel.Reply <| Ok (agg, version)
        { repo with Cache = cache }

    let put (lg: DiagnoseLog.Logger) repo aggId agg version =
        lg.Trace "Put aggregate [%s] back to repository." aggId
        let (queue, state) = repo.Cache.[aggId]
        match !state with
        | Empty -> state := Available (agg, version, DateTime.Now.Ticks)
        | Pending _ ->
            let (t, channel) = queue.Dequeue()
            if queue.Count = 0 then state := Empty
            else state := Pending t
            channel.Reply <| Ok (agg, version)
        | Blocked _ ->
            let (t, channel) = queue.Dequeue()
            if DateTime.Now.Ticks - t < repo.Timeout then state := Pending t
            channel.Reply <| Ok (agg, version)
        | Available _ -> failwithf "Status of aggregate [%s] is 'Available'." aggId
        repo

    let refresh (lg: DiagnoseLog.Logger) (repo: T<'agg>) interval =
        lg.Trace "Refresh aggregate cache."
        let now = DateTime.Now.Ticks
        let cache =
            Map.filter (fun _ (queue: Queue<int64 * AsyncReplyChannel<Result< ^agg * int64, string>>>, state) ->
                match !state with
                | Available (_, _, ticks) -> now - ticks < interval
                | Blocked ticks ->
                    if now - ticks > interval then
                        seq { 1 .. queue.Count }
                        |> Seq.iter (fun _ ->
                            let (_, channel) = queue.Dequeue()
                            channel.Reply <| Error "'Blocked' status timeout. "
                        )
                        false
                    else true
                | _ -> true
            ) repo.Cache
        { repo with Cache = cache }

    let scavenge (lg: DiagnoseLog.Logger) interval =
        lg.Trace "Scavenge aggregate snapshot."
        let now = DateTime.Now.Ticks
        snapshot :=
            Map.filter (fun _ (_, _, _, ticks) ->
                now - !ticks < interval
            ) (!snapshot)

    let inline gTake lg repo aggId channel =
        if repo.Cache.ContainsKey aggId then
            fromCache lg repo aggId channel
        else
            let init = (^agg : (static member Initial : ^agg) ())
            fromStore lg repo aggId init 0L channel

    let gPut lg repo aggId agg version = put lg repo aggId agg version

    let inline sTake lg repo aggId channel =
        if repo.Cache.ContainsKey aggId then
            fromCache lg repo aggId channel
        elif (!snapshot).ContainsKey aggId then
            let (agg, version, _, _) = (!snapshot).[aggId]
            fromStore lg repo aggId agg version channel
        else
            let init = (^agg : (static member Initial : ^agg) ())
            fromStore lg repo aggId init 0L channel

    let sPut (lg: DiagnoseLog.Logger) threshold repo aggId agg version =
        if (!snapshot).ContainsKey aggId then
            let (_, _, step, ticks) = (!snapshot).[aggId]
            if version - step > threshold then
                let step = step + threshold
                ticks := DateTime.Now.Ticks
                snapshot := (!snapshot) |> Map.remove aggId |> Map.add aggId (agg, version, step, ticks)
                lg.Trace "Build snapshot: aggregate [%s], version %d, step %d." aggId version step
        elif version > threshold then
            let step = version / threshold * threshold
            snapshot := (!snapshot).Add (aggId, (agg, version, step, ref DateTime.Now.Ticks))
            lg.Trace "Build snapshot: aggregate [%s], version %d, step %d." aggId version step
        put lg repo aggId agg version