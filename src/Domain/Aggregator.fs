namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text.Json


module Aggregator =

    type Writer = Guid option -> string -> Guid -> uint64 -> string -> ReadOnlyMemory<byte> -> unit
    type Reader = string -> Guid -> seq<string * ReadOnlyMemory<byte>>

    type Msg<'agg when Agg<'agg>> =
        | Refresh
        | Register of string * ('agg -> ReadOnlyMemory<byte> -> unit)
        | Create of
            Guid option *
            ('agg -> unit) *
            ('agg -> string * ReadOnlyMemory<byte>) *
            AsyncReplyChannel<Result<'agg, exn>>
        | Apply of
            Guid option *
            Guid *
            ('agg -> unit) *
            ('agg -> string * ReadOnlyMemory<byte>) *
            AsyncReplyChannel<Result<'agg, exn>>

    let inline validate<'agg, 'chg when Chg<'agg, 'chg>> (chg: 'chg) (agg: 'agg) = chg.Validate agg

    let inline execute<'agg, 'chg when Chg<'agg, 'chg>> (chg: 'chg) (agg: 'agg) =
        chg.Execute agg
        typeof<'chg>.FullName, JsonSerializer.SerializeToUtf8Bytes chg |> ReadOnlyMemory

    let inline init<'agg when Agg<'agg>>
        ([<InlineIfLambda>] (creator: Guid -> 'agg))
        ([<InlineIfLambda>] writer: Writer)
        ([<InlineIfLambda>] reader: Reader)
        (capacity: int)
        refresh
        =
        let aggType = typeof<'agg>.FullName
        let replayer = Dictionary<string, 'agg -> ReadOnlyMemory<byte> -> unit>()
        let repository = Dictionary<Guid, DateTime * 'agg>(capacity)

        let createTimer (interval: float) work =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add work
            async { timer.Start() }

        let inline replay aggType aggId agg =
            reader aggType aggId
            |> Seq.iter (fun (chgType, chgData) ->
                let act = replayer[chgType]
                act agg chgData
                agg.Next())

        let inline handle traceId aggId validate execute (channel: AsyncReplyChannel<Result<'agg, exn>>) =
            try
                let agg = creator aggId
                replay aggType aggId agg
                validate agg
                let chgType, chgData = execute agg
                writer traceId aggType agg.Id agg.Revision chgType chgData
                agg.Next()
                channel.Reply <| Ok agg
                repository.Add(agg.Id, (DateTime.Now, agg))
            with ex ->
                channel.Reply <| Error ex

        let agent =
            MailboxProcessor<Msg<'agg>>.Start
            <| fun inbox ->
                let rec loop () =
                    async {
                        match! inbox.Receive() with
                        | Refresh ->
                            for (KeyValue(aggId, (dt, _))) in repository do
                                if (DateTime.Now - dt).TotalSeconds > refresh then
                                    repository.Remove(aggId) |> ignore
                        | Register(chgType, act) -> replayer[chgType] <- act
                        | Create(traceId, validate, execute, channel) ->
                            try
                                let agg = creator <| Guid.NewGuid()
                                validate agg
                                let chgType, chgData = execute agg
                                writer traceId aggType agg.Id agg.Revision chgType chgData
                                agg.Next()
                                channel.Reply <| Ok agg
                            with ex ->
                                channel.Reply <| Error ex
                        | Apply(traceId, aggId, validate, execute, channel) ->
                            if repository.ContainsKey aggId then
                                try
                                    let _, agg = repository[aggId]
                                    validate agg

                                    try
                                        let chgType, chgData = execute agg

                                        try
                                            writer traceId aggType agg.Id agg.Revision chgType chgData
                                            agg.Next()
                                            channel.Reply <| Ok agg
                                            repository[aggId] <- DateTime.Now, agg
                                        with _ ->
                                            repository.Remove(aggId) |> ignore
                                            handle traceId aggId validate execute channel
                                    with ex ->
                                        repository.Remove(aggId) |> ignore
                                        raise ex
                                with ex ->
                                    channel.Reply <| Error ex
                            else
                                handle traceId aggId validate execute channel

                        return! loop ()
                    }

                loop ()

        createTimer (refresh * 1000.0) (fun _ -> agent.Post Refresh) |> Async.Start
        agent

    let inline register<'agg, 'rep when Rep<'agg, 'rep>> (agent: MailboxProcessor<Msg<'agg>>) (rep: 'rep) =
        agent.Post <| Register(rep.FullName, rep.Act)

    let inline create<'agg, 'chg when Chg<'agg, 'chg>> (agent: MailboxProcessor<Msg<'agg>>) (traceId) (chg: 'chg) =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Create(traceId, validate chg, execute chg, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }

    let inline apply<'agg, 'chg when Chg<'agg, 'chg>> (agent: MailboxProcessor<Msg<'agg>>) traceId aggId (chg: 'chg) =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Apply(traceId, aggId, validate chg, execute chg, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }
