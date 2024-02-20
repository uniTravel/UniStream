namespace UniStream.Infrastructure

open System
open EventStore.Client
open FSharp.Control


module Stream =

    type T = Client of EventStoreClient

    let create (settings: EventStoreClientSettings) =
        Client <| new EventStoreClient(settings)

    let close (Client client) = client.Dispose()

    let write (Client client) (traceId: Guid) aggType (aggId: Guid) revision evtType (evtData: byte array) =
        let stream = aggType + "-" + aggId.ToString()
        let data = EventData(Uuid.NewUuid(), evtType, evtData)

        client.AppendToStreamAsync(stream, StreamRevision revision, [ data ])
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> ignore

    let read (Client client) (aggType: string) (aggId: Guid) =
        let stream = aggType + "-" + aggId.ToString()

        client.ReadStreamAsync(Direction.Forwards, stream, StreamPosition.Start)
        |> AsyncSeq.ofAsyncEnum
        |> AsyncSeq.map (fun x -> x.Event.EventType, x.Event.Data.ToArray())
        |> AsyncSeq.toListSynchronously
