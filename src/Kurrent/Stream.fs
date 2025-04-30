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
        try
            let stream = aggType + "-" + aggId.ToString()
            let data = EventData(Uuid.FromGuid comId, evtType, evtData)
            client.AppendToStreamAsync(stream, StreamRevision revision, [ data ]).Wait()
        with ex ->
            logger.LogError $"Write {evtType} of {aggId} error: {ex.Message}"
            raise <| WriteException($"Write {evtType} of {aggId} error", ex)

    let read (aggId: Guid) =
        try
            task {
                let stream = aggType + "-" + aggId.ToString()
                let result = ResizeArray<string * ReadOnlyMemory<byte>>()
                let r = client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
                use e = r.GetAsyncEnumerator()

                while! e.MoveNextAsync() do
                    result.Add(e.Current.Event.EventType, e.Current.Event.Data)

                return List.ofSeq result
            }
            |> Async.AwaitTask
            |> Async.RunSynchronously
        with ex ->
            logger.LogError $"Read strem of {aggId} error: {ex.Message}"
            raise <| ReadException($"Read strem of {aggId} error", ex)

    let restore (ch: HashSet<Guid>) (latest: int) =
        task {
            let targetTime = DateTime.UtcNow.AddMinutes -latest
            let stream = "$ce-" + aggType
            let r = client.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End)
            use e = r.GetAsyncEnumerator()
            let mutable hasNext = true
            let mutable shoudRun = true
            let! moved = e.MoveNextAsync()
            hasNext <- moved

            while hasNext && shoudRun do
                e.Current.Event.EventId.ToGuid() |> ch.Add |> ignore
                shoudRun <- e.Current.Event.Created > targetTime
                let! moved = e.MoveNextAsync()
                hasNext <- moved

            logger.LogInformation $"{ch.Count} comId of {aggType} cached"
            return List.ofSeq ch
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously

    interface IStream<'agg> with
        member _.Reader = read
        member _.Writer = write
        member _.Restore = restore
