namespace UniStream.Domain

open System
open System.Collections.Generic


module Fetcher =

    type Msg<'agg> =
        | Refresh
        | Get of Guid * uint64 * AsyncReplyChannel<Result<'agg, string>>

    type Agg<'agg when 'agg :> Aggregate and 'agg: (member ReplayEvent: (string -> ReadOnlyMemory<byte> -> unit))> =
        'agg

    let inline agent<'agg when Agg<'agg>>
        ([<InlineIfLambda>] creator)
        ([<InlineIfLambda>] fetcher)
        (capacity: int)
        refresh
        =
        let aggType = typeof<'agg>.FullName + "-"

        let createTimer (interval: float) handler =
            let timer = new Timers.Timer(interval)
            timer.AutoReset <- true
            timer.Elapsed.Add handler
            async { timer.Start() }

        let inline handler agg stream start maxCount =
            async {
                let! evs = fetcher stream start maxCount

                return
                    evs
                    |> Seq.fold
                        (fun (agg: 'agg) (evType, ev) ->
                            agg.ReplayEvent evType ev
                            agg)
                        agg
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
                        | Get(aggId, revision, channel) ->
                            try
                                if repository.ContainsKey aggId then
                                    let _, stream, agg = repository[aggId]

                                    if agg.Revision = revision then
                                        channel.Reply <| Ok agg
                                    elif agg.Revision > revision then
                                        let agg = creator aggId
                                        let! agg = handler agg stream 0UL (revision + 1UL)
                                        channel.Reply <| Ok agg
                                        repository[aggId] <- DateTime.Now, stream, agg
                                    else
                                        let! agg = handler agg stream (agg.Revision + 1UL) (revision - agg.Revision)

                                        if agg.Revision = revision then
                                            channel.Reply <| Ok agg
                                        else
                                            channel.Reply
                                            <| Error
                                                $"Received revision is %d{revision}, latest stored revision is %d{agg.Revision}."

                                        repository[aggId] <- DateTime.Now, stream, agg
                                else
                                    let agg = creator aggId
                                    let stream = aggType + aggId.ToString()
                                    let! agg = handler agg stream 0UL (revision + 1UL)

                                    if agg.Revision = revision then
                                        channel.Reply <| Ok agg
                                    else
                                        channel.Reply
                                        <| Error
                                            $"Received revision is %d{revision}, latest stored revision is %d{agg.Revision}."

                                    repository.Add(aggId, (DateTime.Now, stream, agg))
                            with ex ->
                                channel.Reply <| Error ex.Message

                        return! loop repository
                    }

                loop (Dictionary<Guid, DateTime * string * 'agg>(capacity))

        createTimer (refresh * 1000.0) (fun _ -> agent.Post Refresh) |> Async.Start
        agent

    let inline build<'agg when Agg<'agg>> ([<InlineIfLambda>] creator) ([<InlineIfLambda>] fetcher) capacity refresh =
        if capacity < 5000 || capacity > 1000000 then
            invalidArg (nameof capacity) "Capacity of repository must between 5000~100000."

        if refresh < 1.0 || refresh > 7200.0 then
            invalidArg (nameof refresh) "Interval for refresh cache must between 1~7200 seconds."

        agent<'agg> creator fetcher capacity refresh

    let inline get<'agg when Agg<'agg>> (agent: MailboxProcessor<Msg<'agg>>) aggId revision =
        async {
            match! agent.PostAndAsyncReply <| fun channel -> Get(aggId, revision, channel) with
            | Ok agg -> return agg
            | Error err -> return failwith $"Get aggregate failed: %s{err}"
        }
