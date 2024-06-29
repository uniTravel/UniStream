namespace UniStream.Domain

open System
open System.Collections.Generic
open Microsoft.Extensions.Logging
open EventStore.Client


[<Sealed>]
type Stream<'agg when 'agg :> Aggregate>(logger: ILogger<Stream<'agg>>, client: IClient) =
    let aggType = typeof<'agg>.FullName
    let client = client.Client

    let write (aggId: Guid) (comId: Guid) revision evtType (evtData: byte array) =
        let stream = aggType + "-" + aggId.ToString()
        let data = EventData(Uuid.FromGuid comId, evtType, evtData)
        client.AppendToStreamAsync(stream, StreamRevision revision, [ data ]).Wait()

    let read (aggId: Guid) =
        let stream = aggType + "-" + aggId.ToString()

        let e =
            client
                .ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
                .GetAsyncEnumerator()

        [ while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
              yield e.Current.Event.EventType, e.Current.Event.Data ]

    let restore (ch: HashSet<Guid>) count =
        let count = int64 count
        let stream = "$ce-" + aggType

        let r =
            client.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, count, true)

        let cached =
            match r.ReadState.Result with
            | ReadState.Ok ->
                let e = r.GetAsyncEnumerator()

                [ while (let vt = e.MoveNextAsync() in if vt.IsCompleted then vt.Result else vt.AsTask().Result) do
                      let comId = e.Current.Event.EventId.ToGuid()
                      ch.Add(comId) |> ignore
                      yield comId ]
            | _ -> []

        logger.LogInformation($"{cached.Length} comId of {aggType} cached")
        cached

    interface IStream<'agg> with
        member _.Reader = read
        member _.Writer = write
        member _.Restore = restore
