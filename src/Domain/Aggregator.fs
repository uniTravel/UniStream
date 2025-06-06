namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text.Json
open System.Threading


module Aggregator =

    type Msg<'agg when 'agg :> Aggregate> =
        | Register of string * ('agg -> ReadOnlyMemory<byte> -> unit)
        | Create of Guid * Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<ComResult>
        | Apply of Guid * Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<ComResult>
        | Get of Guid * AsyncReplyChannel<Result<'agg, exn>>

    let inline validate<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) = com.Validate agg

    let inline execute<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (com: 'com) (agg: 'agg) =
        let evt = com.Execute agg
        evt.Apply agg
        typeof<'evt>.FullName, JsonSerializer.SerializeToUtf8Bytes evt

    let inline create<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (agent: MailboxProcessor<Msg<'agg>>)
        aggId
        comId
        (com: 'com)
        =
        agent.PostAndAsyncReply
        <| fun channel -> Create(aggId, comId, validate com, execute com, channel)

    let inline apply<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (agent: MailboxProcessor<Msg<'agg>>)
        aggId
        comId
        (com: 'com)
        =
        agent.PostAndAsyncReply
        <| fun channel -> Apply(aggId, comId, validate com, execute com, channel)

    let inline get<'agg when 'agg :> Aggregate> (agent: MailboxProcessor<Msg<'agg>>) aggId =
        async {
            match! agent.PostAndAsyncReply <| fun channel -> Get(aggId, channel) with
            | Ok agg -> return agg
            | Error ex -> return raise ex
        }

    let inline register<'agg, 'rep when Rep<'agg, 'rep>> (agent: MailboxProcessor<Msg<'agg>>) (rep: 'rep) =
        agent.Post <| Register(rep.FullName, rep.Act)

    let inline init<'agg, 'stream when 'agg :> Aggregate and 'stream :> IStream<'agg>>
        ([<InlineIfLambda>] creator: Guid -> 'agg)
        (token: CancellationToken)
        (stream: 'stream)
        (options: AggregateOptions)
        =
        let capacity = options.Capacity
        let multiple = options.Multiple
        let half = capacity >>> 1
        let upper = capacity * multiple + half
        let ch = HashSet<Guid>()
        let replayer = Dictionary<string, 'agg -> ReadOnlyMemory<byte> -> unit>()
        let repository = Dictionary<Guid, 'agg> capacity

        let reduce ao co =
            let ao = List.distinct ao
            let aol, aor = List.splitAt half ao
            let col, cor = List.splitAt half co

            aor
            |> List.iter (fun id ->
                if repository.ContainsKey id then
                    repository.Remove id |> ignore)

            cor |> List.iter (fun id -> ch.Remove id |> ignore)
            aol, col

        let checkRepo ao co =
            if repository.Count = capacity then reduce ao co else ao, co

        let checkOp ao co =
            if List.length ao = upper then reduce ao co else ao, co

        let inline replay aggId agg =
            stream.Reader aggId
            |> List.iter (fun (evtType, evtData) ->
                try
                    let act = replayer[evtType]
                    act agg evtData
                    agg.Next()
                with ex ->
                    raise <| ReplayException($"Replay {evtType} error", ex))

        let inline handle (agg: 'agg) comId validate execute (channel: AsyncReplyChannel<ComResult>) =
            validate agg
            let evtType, evtData = execute agg
            stream.Writer agg.Id comId agg.Revision evtType evtData
            agg.Next()
            channel.Reply Success

        (fun (inbox: MailboxProcessor<Msg<'agg>>) ->
            let rec loop (ao, co) =
                async {
                    match! inbox.Receive() with
                    | Register(evtType, act) -> replayer[evtType] <- act
                    | Create(aggId, comId, validate, execute, channel) ->
                        if ch.Contains comId then
                            channel.Reply Duplicate
                        else
                            try
                                let agg = creator aggId
                                handle agg comId validate execute channel
                                repository.Add(agg.Id, agg)
                                ch.Add comId |> ignore
                                return! checkRepo (aggId :: ao) (comId :: co) |> loop
                            with ex ->
                                channel.Reply <| Fail ex
                    | Apply(aggId, comId, validate, execute, channel) ->
                        if ch.Contains comId then
                            channel.Reply Duplicate
                        elif repository.ContainsKey aggId then
                            try
                                let agg = repository[aggId]
                                handle agg comId validate execute channel
                                ch.Add comId |> ignore
                                return! checkOp (aggId :: ao) (comId :: co) |> loop
                            with ex ->
                                channel.Reply <| Fail ex
                        else
                            try
                                let agg = creator aggId
                                replay aggId agg
                                handle agg comId validate execute channel
                                repository.Add(agg.Id, agg)
                                ch.Add comId |> ignore
                                return! checkRepo (aggId :: ao) (comId :: co) |> loop
                            with ex ->
                                channel.Reply <| Fail ex
                    | Get(aggId, channel) ->
                        if repository.ContainsKey aggId then
                            channel.Reply <| Ok repository[aggId]
                        else
                            try
                                let agg = creator aggId
                                replay aggId agg
                                channel.Reply <| Ok agg
                            with ex ->
                                channel.Reply <| Error ex

                    return! loop (ao, co)
                }

            loop ([], stream.Restore ch options.Latest)
         , token)
        |> MailboxProcessor<Msg<'agg>>.Start
