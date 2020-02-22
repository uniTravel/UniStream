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
        { Get: Guid -> int64 -> (Guid * string * byte[])[] * int64
          Timeout: int64
          Map: Map<Guid, Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref> }

    let empty get timeout =
        { Get = get; Timeout = timeout; Map = Map.empty }

    let inline refresh aggId agg version (get: Guid -> int64 -> (Guid * string * byte[])[] * int64) =
        let (events, version) = get aggId version
        match events with
        | [||] -> agg, version
        | _ ->
            let agg =
                Array.fold (fun agg elem ->
                    let (_, evType, evBytes) = elem
                    (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType evBytes
                ) agg events
            agg, version

    let inline take repo aggId (channel: AsyncReplyChannel<Result< ^agg * int64, string>>) =
        if repo.Map.ContainsKey aggId then
            let (queue, state) = repo.Map.[aggId]
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
            | Blocked _ -> failwithf "聚合%A，状态为Blocked。" aggId
            repo
        else
            let (events, version) = repo.Get aggId 0L
            let init = (^agg : (static member Initial : ^agg) ())
            let map = repo.Map.Add (aggId, (new Queue<int64 * AsyncReplyChannel<Result< ^agg * int64, string>>>(), ref Empty))
            match events with
            | [||] -> channel.Reply <| Ok (init, -1L)
            | _ ->
                let agg =
                    Array.fold (fun agg elem ->
                        let (_, evType, evBytes) = elem
                        (^agg : (member ApplyEvent : (string -> byte[] -> ^agg)) agg) evType evBytes
                    ) init events
                channel.Reply <| Ok (agg, version)
            { repo with Map = map }

    let put repo aggId agg version =
        let (queue, state) = repo.Map.[aggId]
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
        | Available _ -> failwithf "聚合%A，状态为Available。" aggId
        repo

    let scavenge (repo: T<'agg>) (interval: int64) =
        let now = DateTime.Now.Ticks
        let map =
            Map.filter (fun _ (queue: Queue<int64 * AsyncReplyChannel<Result< ^agg * int64, string>>>, state) ->
                match !state with
                | Available (_, _, ticks) -> now - ticks < interval
                | Blocked ticks ->
                    if now - ticks > interval then
                        seq { 1 .. queue.Count }
                        |> Seq.iter (fun _ ->
                            let (_, channel) = queue.Dequeue()
                            channel.Reply <| Error "处于Blocked状态超时。"
                        )
                        false
                    else true
                | _ -> true
            ) repo.Map
        { repo with Map = map }