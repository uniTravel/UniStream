namespace UniStream.Domain

open System
open System.Text.Json


module Aggregator =

    type Accessor<'agg> =
        | Take of MetaTrace.T * AsyncReplyChannel<Result<'agg, string>>
        | Save of 'agg * MetaTrace.T * byte[]
        | Put of 'agg * MetaTrace.T

    type Config =
        { AggType: string
          BlockTicks: int64
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: (Guid -> (byte[] * byte[])[])
          EsFunc: (Guid -> string -> byte[] -> byte[] -> unit) }

    type T<'agg> =
        { Config: Config
          Agent: MailboxProcessor<Accessor<'agg>> }

    let agent<'agg> { BlockTicks = blockTicks; DomainLog = ld; DiagnoseLog = lg; Get = get; EsFunc = esFunc } =
        MailboxProcessor<Accessor<'agg>>.Start <| fun inbox ->
            let rec loop repo = async {
                match! inbox.Receive() with
                | Take (metaTrace, channel) ->
                    try
                        let newRepo = Repository.take repo metaTrace.AggregateId channel
                        return! loop newRepo
                    with ex ->
                        channel.Reply <| Error ex.Message
                        return! loop repo
                | Save (agg', metaTrace, delta) ->
                    try
                        let newRepo = Repository.save repo agg' metaTrace delta
                        ld.Success metaTrace "保存聚合成功。"
                        return! loop newRepo
                    with ex ->
                        ld.Fail metaTrace "保存聚合失败：%s" ex.Message
                        lg.Error ex.StackTrace "保存聚合失败：%s" ex.Message
                        return! loop repo
                | Put (agg, metaTrace) ->
                    try
                        let newRepo = Repository.put repo agg metaTrace
                        ld.Fail metaTrace "放回聚合成功。"
                        return! loop newRepo
                    with ex ->
                        ld.Fail metaTrace "放回聚合失败：%s" ex.Message
                        lg.Error ex.StackTrace "放回聚合失败：%s" ex.Message
                        return! loop repo
            }
            loop <| Repository.empty get esFunc blockTicks

    let create<'agg> get esFunc ldFunc lgFunc blockSeconds =
        let aggType = typeof<'agg>.FullName
        let cfg =
            { AggType = aggType
              BlockTicks = blockSeconds * 10000000L
              DomainLog = DomainLog.logger aggType ldFunc
              DiagnoseLog = DiagnoseLog.logger aggType lgFunc
              Get = get aggType
              EsFunc = esFunc aggType }
        { Config = cfg; Agent = agent<'agg> cfg }

    let apply apply delta { Config = cfg; Agent = agent } metaTrace = async {
        let { DomainLog = ld; DiagnoseLog = lg } = cfg
        ld.Process metaTrace "开始。"
        match! agent.PostAndAsyncReply (fun channel -> Take (metaTrace, channel)) with
        | Ok agg ->
            ld.Process metaTrace "取到聚合。"
            try
                let agg' = apply agg
                ld.Process metaTrace "执行命令成功。"
                agent.Post <| Save (agg', metaTrace, delta)
            with ex ->
                ld.Fail metaTrace "执行命令出错：%s" ex.Message
                lg.Error ex.StackTrace "执行命令出错：%s" ex.Message
                agent.Post <| Put (agg, metaTrace)
        | Error err -> ld.Fail metaTrace "取出聚合出错：%s" err
    }

    let inline applyCommand t metaTrace command = async {
        let delta = Command.asBytes command
        do! apply (^c : (member Apply: ('agg -> 'agg)) command) delta t metaTrace
    }

    let inline applyRaw t aggId cBytes f = async {
        let span = ReadOnlySpan cBytes
        let d = JsonSerializer.Deserialize< ^d> span
        let command = f d
        let meta = MetaTrace.create< ^d> aggId
        do! applyCommand t meta command
    }