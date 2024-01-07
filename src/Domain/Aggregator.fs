namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text.Json


module Aggregator =

    type Msg<'agg when Agg<'agg>> =
        | Refresh
        | Register of string * ('agg -> ReadOnlyMemory<byte> -> unit)
        | Create of ('agg -> unit) * ('agg -> string * ReadOnlyMemory<byte>) * AsyncReplyChannel<Result<'agg, exn>>
        | Apply of
            Guid *
            ('agg -> unit) *
            ('agg -> string * ReadOnlyMemory<byte>) *
            AsyncReplyChannel<Result<'agg, exn>>

    let inline validate<'agg, 'com when Com<'agg, 'com>> (com: 'com) (agg: 'agg) = com.Validate agg

    let inline execute<'agg, 'com when Com<'agg, 'com>> (com: 'com) (agg: 'agg) =
        com.Execute agg
        typeof<'com>.FullName, JsonSerializer.SerializeToUtf8Bytes com |> ReadOnlyMemory

    let inline init<'agg when Agg<'agg>>
        ([<InlineIfLambda>] (creator: Guid -> 'agg))
        ([<InlineIfLambda>] (writer: string -> Guid -> uint64 -> string -> ReadOnlyMemory<byte> -> unit))
        ([<InlineIfLambda>] (reader: string -> Guid -> seq<string * ReadOnlyMemory<byte>>))
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
            |> Seq.iter (fun (comType, comData) ->
                let act = replayer[comType]
                act agg comData
                agg.Next())

        let inline handle aggId validate execute (channel: AsyncReplyChannel<Result<'agg, exn>>) =
            try
                let agg = creator aggId
                replay aggType aggId agg
                validate agg
                let comType, comData = execute agg
                writer aggType agg.Id agg.Revision comType comData
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
                        | Register(comType, act) -> replayer[comType] <- act
                        | Create(validate, execute, channel) ->
                            try
                                let agg = creator <| Guid.NewGuid()
                                validate agg
                                let comType, comData = execute agg
                                writer aggType agg.Id agg.Revision comType comData
                                agg.Next()
                                channel.Reply <| Ok agg
                                repository.Add(agg.Id, (DateTime.Now, agg))
                            with ex ->
                                channel.Reply <| Error ex
                        | Apply(aggId, validate, execute, channel) ->
                            if repository.ContainsKey aggId then
                                try
                                    let _, agg = repository[aggId]
                                    validate agg

                                    try
                                        let comType, comData = execute agg

                                        try
                                            writer aggType agg.Id agg.Revision comType comData
                                            agg.Next()
                                            channel.Reply <| Ok agg
                                            repository[aggId] <- DateTime.Now, agg
                                        with _ ->
                                            repository.Remove(aggId) |> ignore
                                            handle aggId validate execute channel
                                    with ex ->
                                        repository.Remove(aggId) |> ignore
                                        raise ex
                                with ex ->
                                    channel.Reply <| Error ex
                            else
                                handle aggId validate execute channel

                        return! loop ()
                    }

                loop ()

        createTimer (refresh * 1000.0) (fun _ -> agent.Post Refresh) |> Async.Start
        agent

    let inline register<'agg, 'rep when Rep<'agg, 'rep>> (agent: MailboxProcessor<Msg<'agg>>) (rep: 'rep) =
        agent.Post <| Register(rep.FullName, rep.Act)

    let inline create<'agg, 'com when Com<'agg, 'com>> (agent: MailboxProcessor<Msg<'agg>>) (com: 'com) =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Create(validate com, execute com, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }

    let inline apply<'agg, 'com when Com<'agg, 'com>> (agent: MailboxProcessor<Msg<'agg>>) aggId (com: 'com) =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Apply(aggId, validate com, execute com, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }
