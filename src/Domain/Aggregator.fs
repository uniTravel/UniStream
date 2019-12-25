namespace UniStream.Domain

open System


module Aggregator =

    type Accessor<'agg> =
        | Take of Guid * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of Guid * 'agg * int64
        | Scavenge of int64

    type StoreConfig =
        { Get: string -> Guid -> (Guid * string * byte[])[] * int64
          GetFrom: string -> Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: string -> Guid -> int64 -> Guid -> string -> byte[] -> unit
          LdFunc: string -> Guid -> string -> byte[] -> unit
          LgFunc: string -> byte[] -> unit }

    type T<'agg> =
        { AggType: string
          Timeout: int64
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> (Guid * string * byte[])[] * int64
          GetFrom: Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: Guid -> int64 -> Guid -> string -> byte[] -> unit
          Agent: MailboxProcessor<Accessor<'agg>> }

    let config get getFrom esFunc ldFunc lgFunc =
        { Get = get; GetFrom = getFrom; EsFunc = esFunc; LdFunc = ldFunc; LgFunc = lgFunc }

    let inline agent< ^agg when ^agg : (static member Empty : ^agg) and ^agg : (member Apply : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) get timeout =
        MailboxProcessor<Accessor< ^agg>>.Start <| fun inbox ->
            let rec loop repo = async {
                match! inbox.Receive() with
                | Take (aggId, channel) ->
                    try
                        let newRepo = Repository.take repo aggId channel
                        return! loop newRepo
                    with ex ->
                        channel.Reply <| Error ex.Message
                        return! loop repo
                | Put (aggId, agg, version) ->
                    try
                        let newRepo = Repository.put repo aggId agg version
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "放回聚合失败：%s" ex.Message
                        return! loop repo
                | Scavenge interval ->
                    try
                        let newRepo = Repository.scavenge repo interval
                        return! loop newRepo
                    with ex ->
                        lg.Error ex.StackTrace "清扫聚合仓储出错：%s" ex.Message
                        return! loop repo
            }
            loop <| Repository.empty get timeout

    let inline create cfg blockSeconds =
        let createTimer interval handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }
        let aggType = typeof< ^agg>.FullName
        if blockSeconds <= 0L || blockSeconds >= 10L then invalidArg "blockSeconds" "超时锁定的秒数应该介于0~10之间。"
        let timeout = blockSeconds * 10000000L
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let get = cfg.Get aggType
        let getFrom = cfg.GetFrom aggType
        let esFunc = cfg.EsFunc aggType
        let agent = agent< ^agg> lg get timeout
        Async.Start <| createTimer 15000.0 (fun _ -> agent.Post <| Scavenge 150000000L)
        { AggType = aggType; Timeout = timeout; DomainLog = ld; DiagnoseLog = lg; Get = get; GetFrom = getFrom; EsFunc = esFunc; Agent = agent }

    let inline apply t applyEvent aggId traceId deltaType deltaBytes = async {
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Agent = agent; GetFrom = getFrom } = t
        let launch applyEvent aggId agg version traceId deltaType deltaBytes refreshed =
            try
                let agg' = applyEvent agg
                ld.Process aggId traceId "执行命令成功。"
                let version = version + 1L
                try
                    esFunc aggId version traceId deltaType deltaBytes
                    ld.Success aggId traceId "保存事件成功。"
                    agent.Post <| Put (aggId, agg', version)
                    false
                with ex ->
                    lg.Error ex.StackTrace "保存事件失败：%s。" ex.Message
                    if not refreshed then true
                    else
                        ld.Fail aggId traceId "保存事件失败：%s" ex.Message
                        agent.Post <| Put (aggId, agg, version)
                        false
            with ex ->
                ld.Fail aggId traceId "执行命令出错：%s。" ex.Message
                lg.Error ex.StackTrace "执行命令出错：%s。" ex.Message
                agent.Post <| Put (aggId, agg, version)
                false
        ld.Process aggId traceId "开始。"
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            ld.Process aggId traceId "取到聚合。"
            if launch applyEvent aggId agg version traceId deltaType deltaBytes false then
                let agg, version = Repository.refresh aggId agg version t.GetFrom
                launch applyEvent aggId agg version traceId deltaType deltaBytes true |> ignore
        | Error err -> ld.Fail aggId traceId "取聚合出错：%s" err
    }

    let inline applyCommand t aggId traceId command = async {
        let delta = (^c : (member Value: 'a) command)
        let deltaBytes = Delta.asBytes delta
        let deltaType = delta.GetType().FullName
        do! apply t (^c : (member ApplyEvent: (^agg -> ^agg)) command) aggId traceId deltaType deltaBytes
    }

    let inline applyRaw t aggId traceId deltaType deltaBytes commandCreator = async {
        let command = commandCreator <| Delta.fromBytes deltaBytes
        let trueType = (^c : (member Value: 'a) command).GetType().FullName
        if deltaType <> trueType then invalidArg "deltaType" <| sprintf "边际影响类型应为%s。" trueType
        do! apply t (^c : (member ApplyEvent: (^agg -> ^agg)) command) aggId traceId deltaType deltaBytes
    }