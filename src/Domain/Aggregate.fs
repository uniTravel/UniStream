namespace UniStream.Domain

open System.Text.Json


type AccessRepository<'agg when 'agg :> IAggregate> =
    | Take of MetaTrace.T * AsyncReplyChannel<Result<'agg, string>>
    | Put of 'agg * MetaTrace.T

[<Sealed>]
type Aggregate<'agg when 'agg :> IAggregate> (get, esFunc, ldFunc, lgFunc, blockSeconds) =

    let blockTicks = blockSeconds * 10000000L

    let ld = DomainLog.logger<'agg> ldFunc

    let lg = DiagnoseLog.logger "Aggregate<'agg>" lgFunc

    let agent =
        MailboxProcessor<AccessRepository<'agg>>.Start <| fun inbox ->
            let rec loop (repo: Repository<'agg>) = async {
                match! inbox.Receive () with
                | Take (meta, channel) ->
                    try
                        let id = (meta |> MetaTrace.value).AggregateId
                        let newRepo = repo.Take id get blockTicks channel
                        return! loop newRepo
                    with ex ->
                        channel.Reply <| Error ex.Message
                        return! loop repo
                | Put (agg, meta) ->
                    try
                        let newRepo = repo.Put agg blockTicks
                        ld.Process meta "放回聚合成功。"
                        return! loop newRepo
                    with ex ->
                        ld.Fail meta "放回聚合失败：%s" ex.Message
                        lg.Error ex.StackTrace "放回聚合失败：%s" ex.Message
                        return! loop repo
            }
            loop Repository.empty

    let trigger (agg: 'agg) (event: IDomainEvent<'v, 'agg>) =
        try Ok <| event.Apply agg
        with ex -> Error ex.Message

    let apply meta (agg': 'agg, version: int, event) =
        try
            let m = MetaTrace.asBytes meta
            let v = MetaTrace.value meta
            let e = JsonSerializer.SerializeToUtf8Bytes event
            esFunc v.AggregateId version v.TraceId v.TypeName m e
            Ok agg'
        with ex -> Error ex.Message

    member _.Apply<'v, 'e when 'v :> IValue and 'e :> IDomainEvent<'v, 'agg>> (meta: MetaTrace.T) (command: IDomainCommand<'v, 'agg, 'e>) = async {
        match! agent.PostAndAsyncReply (fun channel -> Take (meta, channel)) with
        | Ok agg ->
            ld.Process meta "取到聚合。"
            let result =
                Ok command.Convert
                |> Result.bind (trigger agg)
                |> Result.bind (apply meta)
            match result with
            | Ok agg' ->
                ld.Success meta "执行命令成功，放回新聚合。"
                agent.Post <| Put (agg', meta)
            | Error err ->
                agent.Post <| Put (agg, meta)
                ld.Fail meta "执行命令出错：%s" err
        | Error err -> ld.Fail meta "取出聚合出错：%s" err
    }