namespace UniStream.Domain

open System
open System.Collections.Generic


module Aggregator =

    type Msg<'agg> =
        | Refresh
        | Init of string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Apply of Guid * uint64 * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Correct of Guid * uint64 * AsyncReplyChannel<Result<'agg, string>>

    type Agg<'agg
        when 'agg :> Aggregate
        and 'agg: (member ApplyCommand: (string -> ReadOnlyMemory<byte> -> string * ReadOnlyMemory<byte>))
        and 'agg: (member ReplayEvent: (string -> ReadOnlyMemory<byte> -> unit))> = 'agg

    let inline agent<'agg when Agg<'agg>>
        ([<InlineIfLambda>] creator)
        ([<InlineIfLambda>] writer)
        ([<InlineIfLambda>] reader)
        (capacity: int)
        refresh
        =
        let aggType = typeof<'agg>.FullName + "-"

        let createTimer (interval: float) handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }

        let inline handler (agg: 'agg) stream revision cmType cm (channel: AsyncReplyChannel<Result<'agg, string>>) =
            async {
                let apply = agg.ApplyCommand
                let evType, ev = apply cmType cm
                do! writer stream revision evType ev
                channel.Reply <| Ok agg
            }

        let agent =
            MailboxProcessor<Msg<'agg>>.Start
            <| fun inbox ->
                let rec loop (repository: Dictionary<Guid, DateTime * string * 'agg>) =
                    async {
                        match! inbox.Receive() with
                        | Refresh ->
                            for (KeyValue(k, (dt, _, _))) in repository do
                                if (DateTime.Now - dt).TotalSeconds > refresh then
                                    repository.Remove(k) |> ignore
                        | Init(cmType, cm, channel) ->
                            try
                                let agg: 'agg = creator <| Guid.NewGuid()
                                let stream = aggType + agg.Id.ToString()
                                do! handler agg stream UInt64.MaxValue cmType cm channel
                                repository.Add(agg.Id, (DateTime.Now, stream, agg))
                            with ex ->
                                channel.Reply <| Error ex.Message
                        | Apply(aggId, revision, cmType, cm, channel) ->
                            try
                                if repository.ContainsKey aggId then
                                    let _, stream, agg = repository[aggId]

                                    if agg.Revision = revision then
                                        do! handler agg stream revision cmType cm channel
                                        repository[aggId] <- (DateTime.Now, stream, agg)
                                    elif agg.Revision > revision then
                                        channel.Reply
                                        <| Error
                                            $"Received revision is %d{revision}, cached revision is %d{agg.Revision}."
                                    else
                                        let! evs = reader stream agg.Revision

                                        let agg: 'agg =
                                            evs
                                            |> Seq.fold
                                                (fun agg (evType, ev) ->
                                                    agg.ReplayEvent evType ev
                                                    agg)
                                                agg

                                        if agg.Revision <> revision then
                                            channel.Reply
                                            <| Error
                                                $"Received revision is %d{revision}, stored revision is %d{agg.Revision}."
                                        else
                                            do! handler agg stream revision cmType cm channel
                                            repository[aggId] <- (DateTime.Now, stream, agg)
                                else
                                    let agg: 'agg = creator aggId
                                    let stream = aggType + aggId.ToString()
                                    let! evs = reader stream 0UL

                                    let agg: 'agg =
                                        evs
                                        |> Seq.fold
                                            (fun agg (evType, ev) ->
                                                agg.ReplayEvent evType ev
                                                agg)
                                            agg

                                    if agg.Revision <> revision then
                                        channel.Reply
                                        <| Error
                                            $"Received revision is %d{revision}, stored revision is %d{agg.Revision}."
                                    else
                                        do! handler agg stream revision cmType cm channel
                                        repository.Add(aggId, (DateTime.Now, stream, agg))
                            with ex ->
                                channel.Reply <| Error ex.Message
                        | Correct(aggId, revision, channel) ->
                            match revision with
                            | 0UL -> channel.Reply <| Error "Received revision must great than 0."
                            | _ ->
                                try
                                    let agg: 'agg = creator aggId
                                    let stream = aggType + aggId.ToString()
                                    let! evs = reader stream 0UL
                                    let evs = List.ofSeq evs
                                    let evType', ev' = evs[evs.Length - 1]

                                    let agg: 'agg =
                                        evs
                                        |> List.take (evs.Length - 1)
                                        |> List.fold
                                            (fun agg (evType, ev) ->
                                                agg.ReplayEvent evType ev
                                                agg)
                                            agg

                                    if agg.Revision = revision - 1UL then
                                        let evType = typeof<'agg>.Name
                                        let ev = Delta.serialize agg
                                        agg.ReplayEvent evType' ev'
                                        agg.ReplayEvent evType ev
                                        do! writer stream revision evType ev
                                        channel.Reply <| Ok agg

                                        if repository.ContainsKey aggId then
                                            repository[aggId] <- (DateTime.Now, stream, agg)
                                        else
                                            repository.Add(aggId, (DateTime.Now, stream, agg))
                                    else
                                        channel.Reply
                                        <| Error
                                            $"Received revision is %d{revision}, proper revision is %d{agg.Revision}."
                                with ex ->
                                    channel.Reply <| Error ex.Message

                        return! loop repository
                    }

                loop <| Dictionary<Guid, DateTime * string * 'agg>(capacity)

        createTimer (refresh * 1000.0) (fun _ -> agent.Post Refresh) |> Async.Start
        agent

    let inline build<'agg when Agg<'agg>>
        ([<InlineIfLambda>] creator)
        ([<InlineIfLambda>] writer)
        ([<InlineIfLambda>] reader)
        capacity
        refresh
        =
        if capacity < 5000 || capacity > 1000000 then
            invalidArg (nameof capacity) "Capacity of repository must between 5000~100000."

        if refresh < 1.0 || refresh > 7200.0 then
            invalidArg (nameof refresh) "Interval for refresh cache must between 1~7200 seconds."

        agent<'agg> creator writer reader capacity refresh

    let inline init<'agg when Agg<'agg>> (agent: MailboxProcessor<Msg<'agg>>) cmType cm =
        async {
            match! agent.PostAndAsyncReply <| fun channel -> Init(cmType, cm, channel) with
            | Ok agg -> return agg
            | Error err -> return failwith $"Apply initial command failed: %s{err}"
        }

    let inline apply<'agg when Agg<'agg>> (agent: MailboxProcessor<Msg<'agg>>) aggId revision cmType cm =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Apply(aggId, revision, cmType, cm, channel)
            with
            | Ok agg -> return agg
            | Error err -> return failwith $"Apply command failed: %s{err}"
        }

    let inline correct<'agg when Agg<'agg>> (agent: MailboxProcessor<Msg<'agg>>) aggId revision =
        async {
            match! agent.PostAndAsyncReply <| fun channel -> Correct(aggId, revision, channel) with
            | Ok agg -> return agg
            | Error err -> return failwith $"Correct aggregate failed: %s{err}"
        }
