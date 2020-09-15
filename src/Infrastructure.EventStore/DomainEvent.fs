namespace UniStream.Infrastructure.EventStore

open System.Linq
open EventStore.Client


module DomainEvent =

    let write (client: EventStoreClient) aggType aggKey version eData =
        let streamName = aggType + aggKey
        let eventData = eData |> Seq.map (fun (evType, data, metadata) -> EventData (Uuid.NewUuid(), evType, data, metadata))
        client.AppendToStreamAsync (streamName, StreamRevision version, eventData) |> Async.AwaitTask |> Async.Ignore

    let get (client: EventStoreClient) aggType aggKey version = async {
        let streamName = aggType + aggKey
        let result = client.ReadStreamAsync (Direction.Forwards, streamName, StreamPosition(version))
        match! result.ReadState |> Async.AwaitTask with
        | ReadState.StreamNotFound -> return Seq.empty
        | _ -> return result.Select(fun e -> e.Event.EventNumber.ToUInt64(), e.Event.EventType, e.Event.Data).ToEnumerable() }