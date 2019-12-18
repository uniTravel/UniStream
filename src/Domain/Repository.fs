namespace UniStream.Domain

open System
open System.Collections.Generic


module Repository =

    type State<'agg> =
        | Available of 'agg
        | Pending of int64
        | Blocked of int64

    type T<'agg> =
        { Get: Guid -> (byte[] * byte[])[]
          EsFunc: Guid -> string -> byte[] -> byte[] -> unit
          Timeout: int64
          M: Map<Guid, Queue<int64 * AsyncReplyChannel<Result<'agg, string>>> * (int * State<'agg>) ref> }

    let refresh repo agg aggId (queue: Queue<int64 * AsyncReplyChannel<Result<'agg, string>>>) item version state =
        let setItem ticks t =
            if DateTime.Now.Ticks - ticks > repo.Timeout then item := version, Blocked t
            else item := version, Pending t
        match queue.Count with
        | 0 -> item := version, Available agg
        | _ ->
            let (t, channel) = queue.Dequeue()
            match state with
            | Pending ticks -> setItem ticks t
            | Blocked ticks -> setItem ticks t
            | Available _ -> failwithf "聚合%A，状态为Available。" aggId
            channel.Reply <| Ok agg
        repo

    let empty get esFunc timeout =
        { Get = get; EsFunc = esFunc; Timeout = timeout; M = Map.empty }

    let take (repo: T<'agg>) (id: Guid) (channel: AsyncReplyChannel<Result<'agg, string>>) =

        repo

    let save repo agg' (metaTrace: MetaTrace.T) delta =
        let (queue, item) = repo.M.[metaTrace.AggregateId]
        let (v, state) = !item
        let version = v + 1
        let e = MetaEvent.create metaTrace version |> MetaEvent.asBytes
        repo.EsFunc metaTrace.TraceId metaTrace.DeltaType delta e
        refresh repo agg' metaTrace.AggregateId queue item version state

    let put repo agg (metaTrace: MetaTrace.T) =
        let (queue, item) = repo.M.[metaTrace.AggregateId]
        let (v, state) = !item
        refresh repo agg metaTrace.AggregateId queue item v state