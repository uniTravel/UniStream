namespace UniStream.Domain

open System


module Aggregator =

    type Accessor<'agg> =
        | Take of Guid * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of Guid * 'agg * int64
        | Scavenge of int64

    type StoreConfig =
        { Get: string -> Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: string -> Guid -> int64 -> (string * byte[])[] -> int64
          LdFunc: string -> string -> byte[] -> unit
          LgFunc: string -> byte[] -> unit }

    type T<'agg> =
        { AggType: string
          Timeout: int64
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: Guid -> int64 -> (string * byte[])[] -> int64
          Agent: MailboxProcessor<Accessor<'agg>> }

    let config get esFunc ldFunc lgFunc =
        { Get = get; EsFunc = esFunc; LdFunc = ldFunc; LgFunc = lgFunc }

    let inline agent< ^agg when ^agg : (static member Initial : ^agg) and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) get timeout =
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
        let aggType = typeof< ^agg>.DeclaringType.FullName
        if blockSeconds <= 0L || blockSeconds >= 10L then invalidArg "blockSeconds" "超时锁定的秒数应该介于0~10之间。"
        let timeout = blockSeconds * 10000000L
        let ld = DomainLog.logger aggType cfg.LdFunc
        let lg = DiagnoseLog.logger aggType cfg.LgFunc
        let get = cfg.Get aggType
        let esFunc = cfg.EsFunc aggType
        let agent = agent< ^agg> lg get timeout
        Async.Start <| createTimer 15000.0 (fun _ -> agent.Post <| Scavenge 150000000L)
        { AggType = aggType; Timeout = timeout; DomainLog = ld; DiagnoseLog = lg; Get = get; EsFunc = esFunc; Agent = agent }

    let inline execute t apply cvType aggId traceId = async {
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Agent = agent; Get = get } = t
        let rec launch apply aggId agg version traceId refreshed =
            try
                let events, agg' = apply agg
                ld.Process cvType aggId traceId "应用命令成功。"
                try
                    let version = esFunc aggId (version + 1L) events
                    ld.Success cvType aggId traceId "保存事件成功。"
                    agent.Post <| Put (aggId, agg', version)
                    (^agg : (member Value: ^v) agg')
                with ex ->
                    lg.Error ex.StackTrace "保存事件失败：%s。" ex.Message
                    if not refreshed then
                        ld.Process cvType aggId traceId "刷新聚合。"
                        let agg, version = Repository.refresh aggId agg (version + 1L) t.Get
                        launch apply aggId agg version traceId true
                    else
                        ld.Fail cvType aggId traceId "保存事件失败：%s" ex.Message
                        agent.Post <| Put (aggId, agg, version)
                        failwith "保存事件失败。"
            with ex ->
                ld.Fail cvType aggId traceId "应用命令出错：%s。" ex.Message
                lg.Error ex.StackTrace "应用命令出错：%s。" ex.Message
                agent.Post <| Put (aggId, agg, version)
                failwith "应用命令出错。"
        ld.Process cvType aggId traceId "开始。"
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            ld.Process cvType aggId traceId "取到聚合。"
            return launch apply aggId agg version traceId false
        | Error err ->
            ld.Fail cvType aggId traceId "取聚合出错：%s" err
            return failwith "取聚合出错。"
    }

    let inline executeCommand t aggId traceId command = async {
        let cvType = (^c : (static member ValueType : string) ())
        let apply = (^c : (member Apply: (^agg -> (string * byte[])[] * ^agg)) command)
        return! execute t apply cvType aggId traceId
    }