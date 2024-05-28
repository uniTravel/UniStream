namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text.Json


module Aggregator =

    type Msg<'agg when 'agg :> Aggregate> =
        | Register of string * ('agg -> ReadOnlyMemory<byte> -> unit)
        | Create of Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<Result<'agg, exn>>
        | Apply of Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<Result<'agg, exn>>

    let inline validate<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) = com.Validate agg

    let inline execute<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) =
        let evt = com.Execute agg
        evt.Apply agg
        typeof<'evt>.FullName, JsonSerializer.SerializeToUtf8Bytes evt

    let inline create<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (agent: MailboxProcessor<Msg<'agg>>)
        aggId
        (com: 'com)
        =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Create(aggId, validate com, execute com, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }

    let inline apply<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (agent: MailboxProcessor<Msg<'agg>>)
        aggId
        (com: 'com)
        =
        async {
            match!
                agent.PostAndAsyncReply
                <| fun channel -> Apply(aggId, validate com, execute com, channel)
            with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }

    let inline register<'agg, 'rep when Rep<'agg, 'rep>> (agent: MailboxProcessor<Msg<'agg>>) (rep: 'rep) =
        agent.Post <| Register(rep.FullName, rep.Act)

    let inline init<'agg, 'stream when 'agg :> Aggregate and 'stream :> IStream>
        ([<InlineIfLambda>] creator: Guid -> 'agg)
        (stream: 'stream)
        (options: AggregateOptions)
        =
        let aggType = typeof<'agg>.FullName
        let capacity = options.Capacity
        let multiple = options.Multiple
        let half = capacity >>> 1
        let upper = capacity * multiple + half
        let replayer = Dictionary<string, 'agg -> ReadOnlyMemory<byte> -> unit>()
        let repository = Dictionary<Guid, 'agg>(capacity)

        let reduce op =
            let op = List.distinct op
            let l, r = List.splitAt half op

            r
            |> List.iter (fun id ->
                if repository.ContainsKey id then
                    repository.Remove(id) |> ignore)

            l

        let checkRepo op =
            if repository.Count = capacity then reduce op else op

        let checkOp op =
            if List.length op = upper then reduce op else op

        let inline replay aggType aggId agg =
            stream.Reader aggType aggId
            |> List.iter (fun (evtType, evtData) ->
                let act = replayer[evtType]
                act agg evtData
                agg.Next())

        let inline handle (agg: 'agg) validate execute (channel: AsyncReplyChannel<Result<'agg, exn>>) =
            validate agg
            let evtType, evtData = execute agg
            stream.Writer aggType agg.Id agg.Revision evtType evtData
            agg.Next()
            channel.Reply <| Ok agg

        let agent =
            MailboxProcessor<Msg<'agg>>.Start
            <| fun inbox ->
                let rec loop op =
                    async {
                        match! inbox.Receive() with
                        | Register(evtType, act) -> replayer[evtType] <- act
                        | Create(aggId, validate, execute, channel) ->
                            try
                                let agg = creator aggId
                                handle agg validate execute channel
                                repository.Add(agg.Id, agg)
                                return! aggId :: op |> checkRepo |> loop
                            with ex ->
                                channel.Reply <| Error ex
                        | Apply(aggId, validate, execute, channel) ->
                            if repository.ContainsKey aggId then
                                try
                                    let agg = repository[aggId]
                                    handle agg validate execute channel
                                    repository[aggId] <- agg
                                    return! aggId :: op |> checkOp |> loop
                                with ex ->
                                    channel.Reply <| Error ex
                            else
                                try
                                    let agg = creator aggId
                                    replay aggType aggId agg
                                    handle agg validate execute channel
                                    repository.Add(agg.Id, agg)
                                    return! aggId :: op |> checkRepo |> loop
                                with ex ->
                                    channel.Reply <| Error ex

                        return! loop op
                    }

                loop []

        agent
