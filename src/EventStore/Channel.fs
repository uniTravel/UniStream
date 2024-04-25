namespace UniStream.Domain

open System
open System.Text.Json
open EventStore.Client


module Channel =

    let subscribe (client: EventStoreClient) (comId: Uuid) evtType (channel: AsyncReplyChannel<Result<unit, exn>>) =
        let mutable s = true
        use sub = client.SubscribeToStream($"$bc-{comId}", FromStream.Start, true)
        let e = sub.Messages.GetAsyncEnumerator()

        while s do
            e.MoveNextAsync().AsTask().Wait()

            match e.Current with
            | :? StreamMessage.Event -> s <- false
            | :? StreamMessage.NotFound -> s <- false
            | :? StreamMessage.Unknown -> s <- false
            | _ -> ()

        match e.Current with
        | :? StreamMessage.Event as ev ->
            if ev.ResolvedEvent.Event.EventType = evtType then
                channel.Reply <| Ok()
            elif ev.ResolvedEvent.Event.EventType = "Fail" then
                let err = JsonSerializer.Deserialize<string> ev.ResolvedEvent.Event.Data.Span
                failwith $"Apply command failed: {err}"
            else
                failwith $"Wrong event type: {ev.ResolvedEvent.Event.EventType}"
        | ev -> failwith $"Unexpected stream message type: {ev}"

    let init (client: IClient) =
        let client = client.Client

        MailboxProcessor<Guid * Uuid * string * EventData * AsyncReplyChannel<Result<unit, exn>>>.Start
        <| fun inbox ->
            let rec loop () =
                async {
                    let! aggId, comId, evtType, data, channel = inbox.Receive()

                    try
                        client.AppendToStreamAsync(aggId.ToString(), StreamState.Any, [ data ]).Wait()
                        subscribe client comId evtType channel
                    with ex ->
                        channel.Reply <| Error ex

                    return! loop ()
                }

            loop ()

    let inline setup<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> aggId (com: 'com) channel =
        let comData = JsonSerializer.SerializeToUtf8Bytes com
        let comId = Uuid.NewUuid()
        let data = EventData(comId, typeof<'com>.FullName, comData)
        let evtType = typeof<'evt>.FullName
        aggId, comId, evtType, data, channel

    let inline send<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (agent: MailboxProcessor<Guid * Uuid * string * EventData * AsyncReplyChannel<Result<unit, exn>>>)
        (aggId: Guid)
        (com: 'com)
        =
        async {
            match! agent.PostAndAsyncReply <| fun channel -> setup aggId com channel with
            | Ok _ -> return ()
            | Error ex -> return raise ex
        }
