namespace UniStream.Infrastructure

open System
open System.Text
open EventStore.Client
open FSharp.Control


type Stream(client: EventStoreClient) =

    /// <summary>聚合事件写入流
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggType">聚合类型全称。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="revision">聚合版本。</param>
    /// <param name="evtType">事件类型。</param>
    /// <param name="evtData">事件数据。</param>
    let write (traceId: Guid option) aggType (aggId: Guid) revision evtType (evtData: byte array) =
        let stream = aggType + "-" + aggId.ToString()

        let data =
            match traceId with
            | Some traceId ->
                let metadata =
                    $"{{\"$correlationId\":\"{traceId}\"}}"
                    |> Encoding.ASCII.GetBytes
                    |> ReadOnlyMemory
                    |> Nullable

                EventData(Uuid.NewUuid(), evtType, evtData, metadata)
            | None -> EventData(Uuid.NewUuid(), evtType, evtData)

        client.AppendToStreamAsync(stream, StreamRevision revision, [ data ])
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore

    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">聚合类型全称。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>聚合事件流</returns>
    let read (aggType: string) (aggId: Guid) =
        let stream = aggType + "-" + aggId.ToString()

        client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
        |> AsyncSeq.ofAsyncEnum
        |> AsyncSeq.map (fun x -> x.Event.EventType, x.Event.Data.ToArray())
        |> AsyncSeq.toListSynchronously

    member _.Write = write

    member _.Read = read

    member _.Close() = client.Dispose()
