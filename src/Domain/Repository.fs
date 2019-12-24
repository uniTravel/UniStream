namespace UniStream.Domain

open System
open System.Collections.Generic


module Repository =

    type State<'agg> =
        | Available of 'agg * int64
        | Empty
        | Pending of int64
        | Blocked

    type T<'agg> =
        { Get: Guid -> (Guid * string * byte[])[] * int64
          Timeout: int64
          Map: Map<Guid, Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref> }

    let empty get timeout =
        { Get = get; Timeout = timeout; Map = Map.empty }

    let inline refresh aggId agg version (getFrom: Guid -> int64 -> (Guid * string * byte[])[] * int64) =
        let (events, version) = getFrom aggId version
        match events with
        | [||] -> agg, version
        | _ ->
            let agg =
                Array.fold (fun agg elem ->
                    let (_, deltaType, deltaBytes) = elem
                    (^agg : (member Apply : (string -> byte[] -> ^agg)) agg) deltaType deltaBytes
                ) agg events
            agg, version

    let inline take repo aggId (channel: AsyncReplyChannel<Result< ^agg * int64, string>>) =
        if repo.Map.ContainsKey aggId then
            let (queue, state) = repo.Map.[aggId]
            match !state with
            | Available (agg, version) ->
                state := Empty
                channel.Reply <| Ok (agg, version)
            | Empty ->
                let t = DateTime.Now.Ticks
                queue.Enqueue (t, channel)
                state := Pending t
            | Pending ticks ->
                let t = DateTime.Now.Ticks
                queue.Enqueue (t, channel)
                if t - ticks > repo.Timeout then state := Blocked
            | Blocked -> failwithf "聚合%A，状态为BLocked。" aggId
            repo
        else
            let (events, version) = repo.Get aggId
            let init = (^agg : (static member Empty : ^agg) ())
            let map = repo.Map.Add (aggId, (new Queue<int64 * AsyncReplyChannel<Result< ^agg * int64, string>>>(), ref Empty))
            match events with
            | [||] -> channel.Reply <| Ok (init, 0L)
            | _ ->
                let agg =
                    Array.fold (fun agg elem ->
                        let (_, deltaType, deltaBytes) = elem
                        (^agg : (member Apply : (string -> byte[] -> ^agg)) agg) deltaType deltaBytes
                    ) init events
                channel.Reply <| Ok (agg, version)
            { repo with Map = map }

    let put repo aggId agg version =
        let (queue, state) = repo.Map.[aggId]
        match !state with
        | Empty -> state := Available (agg, version)
        | Pending _ ->
            let (t, channel) = queue.Dequeue()
            if queue.Count = 0 then state := Empty
            else state := Pending t
            channel.Reply <| Ok (agg, version)
        | Blocked ->
            let (t, channel) = queue.Dequeue()
            if DateTime.Now.Ticks - t < repo.Timeout then state := Pending t
            channel.Reply <| Ok (agg, version)
        | Available _ -> failwithf "聚合%A，状态为Available。" aggId
        repo