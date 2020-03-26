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
        { Get: (Guid -> int64 -> (Guid * string * byte[])[] * int64)
          Logger: DiagnoseLog.Logger
          BlockTicks: int64
          Cache: Map<Guid, Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref>
          Snapshot: Map<Guid, 'agg * int64 * int64 * int64 ref> }

    let empty (lg: DiagnoseLog.Logger) get blockTicks =
        lg.Trace "Initialize aggregate repository."
        { Get = get; Logger = lg; BlockTicks = blockTicks; Cache = Map.empty; Snapshot = Map.empty }

    let inline sync (lg: DiagnoseLog.Logger) aggId agg version (get: Guid -> int64 -> (Guid * string * byte[])[] * int64) =
        lg.Trace "Begin sync aggregate %A, start version is %d." aggId version
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

    let fromCache (repo: T<'agg>) aggId (channel: AsyncReplyChannel<Result<'agg * int64, string>>) =
        let (queue, state) = repo.Cache.[aggId]
        repo.Logger.Trace "Get aggregate [%A] from cache." aggId
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
            if now - ticks > repo.BlockTicks then state := Blocked now
        | Blocked _ -> failwithf "Status of aggregate [%A] is 'Blocked'." aggId
        repo

    let inline fromStore repo aggId agg version (channel: AsyncReplyChannel<Result< ^agg * int64, string>>) =
        repo.Logger.Trace "Begin get aggregate [%A] from stream store, start version is %d." aggId version
        let (events, version) = repo.Get aggId version
        repo.Logger.Trace "Get %d data, current version is %d." events.Length version
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

    let refresh (repo: T<'agg>) interval =
        repo.Logger.Trace "Refresh aggregate cache."
        let now = DateTime.Now.Ticks
        let cache =
            Map.filter (fun _ (queue: Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>>, state) ->
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

    let scavenge (repo: T<'agg>) interval =
        repo.Logger.Trace "Scavenge aggregate snapshot."
        let now = DateTime.Now.Ticks
        let snapshot =
            Map.filter (fun _ (_, _, _, ticks) ->
                now - !ticks < interval
            ) repo.Snapshot
        { repo with Snapshot = snapshot }

    let put repo aggId agg version =
        repo.Logger.Trace "Put aggregate [%A] back to repository." aggId
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
            if DateTime.Now.Ticks - t < repo.BlockTicks then state := Pending t
            channel.Reply <| Ok (agg, version)
        | Available _ -> failwithf "Status of aggregate [%A] is 'Available'." aggId
        repo

    let inline cTake repo aggId channel =
        if repo.Cache.ContainsKey aggId then
            fromCache repo aggId channel
        else
            let init = (^agg : (static member Initial : ^agg) ())
            fromStore repo aggId init 0L channel

    let cPut repo aggId agg version = put repo aggId agg version

    let inline sTake repo aggId channel =
        if repo.Cache.ContainsKey aggId then
            fromCache repo aggId channel
        elif repo.Snapshot.ContainsKey aggId then
            let (agg, version, _, _) = repo.Snapshot.[aggId]
            fromStore repo aggId agg version channel
        else
            let init = (^agg : (static member Initial : ^agg) ())
            fromStore repo aggId init 0L channel

    let sPut threshold repo aggId agg version =
        let repo =
            if repo.Snapshot.ContainsKey aggId then
                let (_, _, step, ticks) = repo.Snapshot.[aggId]
                if version - step > threshold then
                    let step = step + threshold
                    ticks := DateTime.Now.Ticks
                    let snapshot = repo.Snapshot |> Map.remove aggId |> Map.add aggId (agg, version, step, ticks)
                    repo.Logger.Trace "Build snapshot: aggregate [%A], version %d, step %d." aggId version step
                    { repo with Snapshot = snapshot }
                else repo
            elif version > threshold then
                let step = version / threshold * threshold
                let snapshot = repo.Snapshot.Add (aggId, (agg, version, step, ref DateTime.Now.Ticks))
                repo.Logger.Trace "Build snapshot: aggregate [%A], version %d, step %d." aggId version step
                { repo with Snapshot = snapshot }
            else repo
        put repo aggId agg version