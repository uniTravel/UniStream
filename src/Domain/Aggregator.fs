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
          LdFunc: string -> Guid -> string -> byte[] -> unit
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

    let inline agent< ^agg when ^agg : (static member Empty : ^agg) and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))> (lg: DiagnoseLog.Logger) get timeout =
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

    let inline execute t apply aggId traceId = async {
        let { DomainLog = ld; DiagnoseLog = lg; EsFunc = esFunc; Agent = agent; Get = get } = t
        let launch apply aggId agg version traceId refreshed =
            try
                let agg', events = apply agg
                ld.Process aggId traceId "应用命令成功。"
                try
                    let version = esFunc aggId (version + 1L) events
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
                ld.Fail aggId traceId "应用命令出错：%s。" ex.Message
                lg.Error ex.StackTrace "应用命令出错：%s。" ex.Message
                agent.Post <| Put (aggId, agg, version)
                false
        ld.Process aggId traceId "开始。"
        match! agent.PostAndAsyncReply (fun channel -> Take (aggId, channel)) with
        | Ok (agg, version) ->
            ld.Process aggId traceId "取到聚合。"
            if launch apply aggId agg version traceId false then
                let agg, version = Repository.refresh aggId agg (version + 1L) t.Get
                launch apply aggId agg version traceId true |> ignore
        | Error err -> ld.Fail aggId traceId "取聚合出错：%s" err
    }

    let inline executeCommand t aggId traceId command = async {
        do! execute t (^c : (member Apply: (^agg -> ^agg * (string * byte[])[])) command) aggId traceId
    }