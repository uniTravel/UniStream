namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text.Json


module Aggregator =

    type Msg<'agg when Agg<'agg>> =
        | Refresh
        | Register of string * ('agg -> byte array -> unit)
        | Create of Guid * Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<Result<'agg, exn>>
        | Apply of Guid * Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<Result<'agg, exn>>

    let inline validate<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) = com.Validate agg

    let inline execute<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) =
        let evt = com.Execute agg
        evt.Apply agg
        typeof<'evt>.FullName, JsonSerializer.SerializeToUtf8Bytes evt

    let inline init<'agg when Agg<'agg>>
        ([<InlineIfLambda>] (creator: Guid -> 'agg))
        (writer: Guid -> string -> Guid -> uint64 -> string -> byte array -> unit)
        (reader: string -> Guid -> seq<string * byte array>)
        (capacity: int)
        refresh
        =
        let aggType = typeof<'agg>.FullName
        let replayer = Dictionary<string, 'agg -> byte array -> unit>()
        let repository = Dictionary<Guid, DateTime * 'agg>(capacity)

        let createTimer (interval: float) work =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add work
            async { timer.Start() }

        let inline replay aggType aggId agg =
            reader aggType aggId
            |> Seq.iter (fun (evtType, evtData) ->
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
                let rec loop () =
                    async {
                        match! inbox.Receive() with
                        | Refresh ->
                            for (KeyValue(aggId, (dt, _))) in repository do
                                if (DateTime.Now - dt).TotalSeconds > refresh then
                                    repository.Remove(aggId) |> ignore
                        | Register(evtType, act) -> replayer[evtType] <- act
                        | Create(traceId, aggId, validate, execute, channel) ->
                            try
                                let agg = creator aggId
                                handle traceId agg validate execute channel
                                repository.Add(agg.Id, (DateTime.Now, agg))
                            with ex ->
                                channel.Reply <| Error ex
                        | Apply(traceId, aggId, validate, execute, channel) ->
                            if repository.ContainsKey aggId then
                                try
                                    let _, agg = repository[aggId]
                                    handle traceId agg validate execute channel
                                    repository[aggId] <- DateTime.Now, agg
                                with ex ->
                                    channel.Reply <| Error ex
                            else
                                try
                                    let agg = creator aggId
                                    replay aggType aggId agg
                                    handle traceId agg validate execute channel
                                    repository.Add(agg.Id, (DateTime.Now, agg))
                                with ex ->
                                    channel.Reply <| Error ex

                        return! loop ()
                    }

                loop ()

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
