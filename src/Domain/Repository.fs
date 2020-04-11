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
        { Reader: Reader
          Logger: DiagnoseLog.Logger
          BlockTicks: int64
          Cache: Map<string, Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref>
          Snapshot: Map<string, 'agg * int64 * int64 * int64 ref> }

    let inline sync (lg: DiagnoseLog.Logger) id agg version (reader: Reader) =
        lg.Trace "Begin sync aggregate %s, start version is %d." id version
        let (events, version) = reader id version
        lg.Trace "Get %d data, current version is %d." events.Length version
        match events with
        | [||] -> agg, version
        | _ ->
            let agg =
                Array.fold (fun agg elem ->
                    let (evType, evBytes) = elem
                    (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType evBytes
                ) agg events
            agg, version

    let inline fromCache (repo: T< ^agg>) id (channel: AsyncReplyChannel<Result< ^agg * int64, string>>) =
        let (queue, state) = repo.Cache.[id]
        repo.Logger.Trace "Get aggregate [%s] from cache." id
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
        | Blocked _ -> failwithf "Status of aggregate [%s] is 'Blocked'." id
        repo

    let inline fromStore repo id agg version (channel: AsyncReplyChannel<Result< ^agg * int64, string>>) =
        repo.Logger.Trace "Begin get aggregate [%s] from stream store, start version is %d." id version
        let (events, version) = repo.Reader id version
        repo.Logger.Trace "Get %d data, current version is %d." events.Length version
        let cache = repo.Cache.Add (id, (new Queue<int64 * AsyncReplyChannel<Result< ^agg * int64, string>>>(), ref Empty))
        match events with
        | [||] -> channel.Reply <| Ok (agg, version - 1L)
        | _ ->
            let agg =
                Array.fold (fun agg elem ->
                    let (evType, evBytes) = elem
                    (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType evBytes
                ) agg events
            channel.Reply <| Ok (agg, version)
        { repo with Cache = cache }

    let inline refresh (repo: T< ^agg>) interval =
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

    let inline scavenge (repo: T< ^agg>) interval =
        repo.Logger.Trace "Scavenge aggregate snapshot."
        let now = DateTime.Now.Ticks
        let snapshot =
            Map.filter (fun _ (_, _, _, ticks) ->
                now - !ticks < interval
            ) repo.Snapshot
        { repo with Snapshot = snapshot }

    let inline put (repo: T< ^agg>) id agg version =
        repo.Logger.Trace "Put aggregate [%s] back to repository." id
        let (queue, state) = repo.Cache.[id]
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
        | Available _ -> failwithf "Status of aggregate [%s] is 'Available'." id
        repo

    let inline cTake repo id channel =
        if repo.Cache.ContainsKey id then
            fromCache repo id channel
        else
            let init = (^agg : (static member Initial : ^agg) ())
            fromStore repo id init 0L channel

    let inline cPut repo id agg version = put repo id agg version

    let inline sTake repo id channel =
        if repo.Cache.ContainsKey id then
            fromCache repo id channel
        elif repo.Snapshot.ContainsKey id then
            let (agg, version, _, _) = repo.Snapshot.[id]
            fromStore repo id agg version channel
        else
            let init = (^agg : (static member Initial : ^agg) ())
            fromStore repo id init 0L channel

    let inline sPut threshold repo id agg version =
        let repo =
            if repo.Snapshot.ContainsKey id then
                let (_, _, step, ticks) = repo.Snapshot.[id]
                if version - step > threshold then
                    let step = step + threshold
                    ticks := DateTime.Now.Ticks
                    let snapshot = repo.Snapshot |> Map.remove id |> Map.add id (agg, version, step, ticks)
                    repo.Logger.Trace "Build snapshot: aggregate [%s], version %d, step %d." id version step
                    { repo with Snapshot = snapshot }
                else repo
            elif version > threshold then
                let step = version / threshold * threshold
                let snapshot = repo.Snapshot.Add (id, (agg, version, step, ref DateTime.Now.Ticks))
                repo.Logger.Trace "Build snapshot: aggregate [%s], version %d, step %d." id version step
                { repo with Snapshot = snapshot }
            else repo
        put repo id agg version

    let empty (lg: DiagnoseLog.Logger) reader blockTicks =
        lg.Trace "Initialize aggregate repository."
        { Reader = reader; Logger = lg; BlockTicks = blockTicks; Cache = Map.empty; Snapshot = Map.empty }