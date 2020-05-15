namespace UniStream.Domain

open System.Linq
open System.Collections.Generic



module Repository =

    type State<'agg> =
        | Available of 'agg * int64
        | Empty
        | Pending

    type T<'agg> =
        { Reader: Reader
          Logger: DiagnoseLog.Logger
          Capacity: int
          Cache: Dictionary<string, Queue<AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref>
          CacheUsage: string list
          Snapshot: Dictionary<string, 'agg * int64 * int64>
          SnapUsage: string list }

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
        | Available (agg, version) ->
            state := Empty
            channel.Reply <| Ok (agg, version)
            { repo with CacheUsage = id :: repo.CacheUsage }
        | Empty ->
            queue.Enqueue channel
            state := Pending
            repo
        | Pending ->
            queue.Enqueue channel
            repo

    let inline fromStore repo id agg version (channel: AsyncReplyChannel<Result< ^agg * int64, string>>) =
        repo.Logger.Trace "Begin get aggregate [%s] from stream store, start version is %d." id version
        let (events, version) = repo.Reader id version
        repo.Logger.Trace "Get %d data, current version is %d." events.Length version
        repo.Cache.Add (id, (new Queue<AsyncReplyChannel<Result< ^agg * int64, string>>>(), ref Empty))
        match events with
        | [||] -> channel.Reply <| Ok (agg, version - 1L)
        | _ ->
            let agg =
                Array.fold (fun agg elem ->
                    let (evType, evBytes) = elem
                    (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType evBytes
                ) agg events
            channel.Reply <| Ok (agg, version)
        { repo with CacheUsage = id :: repo.CacheUsage }

    let inline refresh (repo: T< ^agg>) =
        repo.Logger.Trace "Refresh aggregate cache."
        let usage = repo.CacheUsage |> List.distinct |> List.truncate 3000
        if repo.Cache.Count > repo.Capacity then
            repo.Cache.Keys.Except usage |> Seq.iter (repo.Cache.Remove >> ignore)
        { repo with CacheUsage = usage }

    let inline scavenge (repo: T< ^agg>) =
        repo.Logger.Trace "Scavenge aggregate snapshot."
        let usage = repo.SnapUsage |> List.distinct |> List.truncate 3000
        if repo.Snapshot.Count > repo.Capacity then
            repo.Snapshot.Keys.Except usage |> Seq.iter (repo.Snapshot.Remove >> ignore)
        { repo with SnapUsage = usage }

    let inline put (repo: T< ^agg>) id agg version =
        repo.Logger.Trace "Put aggregate [%s] back to repository." id
        let (queue, state) = repo.Cache.[id]
        match !state with
        | Empty ->
            state := Available (agg, version)
            repo
        | Pending ->
            if queue.Count = 0 then state := Empty else state := Pending
            queue.Dequeue().Reply <| Ok (agg, version)
            { repo with CacheUsage = id :: repo.CacheUsage }
        | Available _ -> failwithf "Status of aggregate [%s] is 'Available'." id

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
            let (agg, version, _) = repo.Snapshot.[id]
            fromStore repo id agg version channel
        else
            let init = (^agg : (static member Initial : ^agg) ())
            fromStore repo id init 0L channel

    let inline sPut threshold repo id agg version =
        let repo =
            if repo.Snapshot.ContainsKey id then
                let (_, _, step) = repo.Snapshot.[id]
                if version - step > threshold then
                    let step = step + threshold
                    repo.Snapshot.[id] <- (agg, version, step)
                    repo.Logger.Trace "Build snapshot: aggregate [%s], version %d, step %d." id version step
                { repo with SnapUsage = id :: repo.SnapUsage }
            elif version > threshold then
                let step = version / threshold * threshold
                repo.Snapshot.Add (id, (agg, version, step))
                repo.Logger.Trace "Build snapshot: aggregate [%s], version %d, step %d." id version step
                { repo with SnapUsage = id :: repo.SnapUsage }
            else repo
        put repo id agg version

    let empty (lg: DiagnoseLog.Logger) reader capacity =
        lg.Trace "Initialize aggregate repository."
        { Reader = reader
          Logger = lg
          Capacity = capacity
          Cache = Dictionary<string, Queue<AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref>(capacity)
          CacheUsage = []
          Snapshot = Dictionary<string, 'agg * int64 * int64>(capacity)
          SnapUsage = [] }