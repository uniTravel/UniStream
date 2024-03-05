namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text.Json


module Aggregator =

    type Msg<'agg when Agg<'agg>> =
        | Refresh
        | Register of string * ('agg -> byte array -> unit)
        | Create of
            Guid option *
            Guid *
            ('agg -> unit) *
            ('agg -> string * byte array) *
            AsyncReplyChannel<Result<'agg, exn>>
        | Apply of
            Guid option *
            Guid *
            ('agg -> unit) *
            ('agg -> string * byte array) *
            AsyncReplyChannel<Result<'agg, exn>>

    let inline validate<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) = com.Validate agg

    let inline execute<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) =
        let evt = com.Execute agg
        evt.Apply agg
        typeof<'evt>.FullName, JsonSerializer.SerializeToUtf8Bytes evt

    let inline init<'agg when Agg<'agg>>
        ([<InlineIfLambda>] creator: Guid -> 'agg)
        ([<InlineIfLambda>] writer: Guid option -> string -> Guid -> uint64 -> string -> byte array -> unit)
        ([<InlineIfLambda>] reader: string -> Guid -> (string * byte array) list)
        (capacity: int)
        refresh
        =
        let max = Int32.MaxValue >>> 3

        if capacity >= max then
            invalidArg (nameof (capacity)) $"capacity must less than %d{max}"

        let aggType = typeof<'agg>.FullName
        let replayer = Dictionary<string, 'agg -> byte array -> unit>()
        let repository = Dictionary<Guid, 'agg>(capacity)
        let half = capacity >>> 1
        let upper = capacity + half

        let createTimer (interval: float) work =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add work
            async { timer.Start() }

        let reduce op =
            let l, r = List.splitAt half op

            r
            |> List.iter (fun id ->
                if repository.ContainsKey id then
                    repository.Remove(id) |> ignore)

            List.distinct l

        let check op =
            if repository.Count = capacity then reduce op else op

        let inline replay aggType aggId agg =
            reader aggType aggId
            |> List.iter (fun (evtType, evtData) ->
                let act = replayer[evtType]
                act agg evtData
                agg.Next())

        let inline handle traceId (agg: 'agg) validate execute (channel: AsyncReplyChannel<Result<'agg, exn>>) =
            validate agg
            let evtType, evtData = execute agg
            writer traceId aggType agg.Id agg.Revision evtType evtData
            agg.Next()
            channel.Reply <| Ok agg

        let agent =
            MailboxProcessor<Msg<'agg>>.Start
            <| fun inbox ->
                let rec loop op =
                    async {
                        match! inbox.Receive() with
                        | Refresh ->
                            if List.length op > upper then
                                return! reduce op |> loop
                        | Register(evtType, act) -> replayer[evtType] <- act
                        | Create(traceId, aggId, validate, execute, channel) ->
                            try
                                let agg = creator aggId
                                handle traceId agg validate execute channel
                                repository.Add(agg.Id, agg)
                                return! aggId :: op |> check |> loop
                            with ex ->
                                channel.Reply <| Error ex
                        | Apply(traceId, aggId, validate, execute, channel) ->
                            if repository.ContainsKey aggId then
                                try
                                    let agg = repository[aggId]
                                    handle traceId agg validate execute channel
                                    repository[aggId] <- agg
                                    return! aggId :: op |> loop
                                with ex ->
                                    channel.Reply <| Error ex
                            else
                                try
                                    let agg = creator aggId
                                    replay aggType aggId agg
                                    handle traceId agg validate execute channel
                                    repository.Add(agg.Id, agg)
                                    return! aggId :: op |> check |> loop
                                with ex ->
                                    channel.Reply <| Error ex

                        return! loop op
                    }

                loop []

        createTimer (refresh * 1000.0) (fun _ -> agent.Post Refresh) |> Async.Start
        agent

    let inline register<'agg, 'rep when Rep<'agg, 'rep>> (agent: MailboxProcessor<Msg<'agg>>) (rep: 'rep) =
        agent.Post <| Register(rep.FullName, rep.Act)

    let inline create<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (agent: MailboxProcessor<Msg<'agg>>)
        traceId
        aggId
        (com: 'com)
        =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Create(traceId, aggId, validate com, execute com, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }

    let inline apply<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (agent: MailboxProcessor<Msg<'agg>>)
        traceId
        aggId
        (com: 'com)
        =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Apply(traceId, aggId, validate com, execute com, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }
