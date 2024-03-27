namespace UniStream.Domain

open System
open System.Text
open EventStore.Client
open FSharp.Control


[<Sealed>]
type Stream(client: IClient) =
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

        client.Client.AppendToStreamAsync(stream, StreamRevision revision, [ data ])
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore

    let read aggType (aggId: Guid) =
        let stream = aggType + "-" + aggId.ToString()

        client.Client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
        |> AsyncSeq.ofAsyncEnum
        |> AsyncSeq.map (fun x -> x.Event.EventType, x.Event.Data.ToArray())
        |> AsyncSeq.toListSynchronously

    interface IStream with
        member _.Reader = read
        member _.Writer = write
