namespace UniStream.Infrastructure.EventStore

open System.Linq
open System.Threading.Tasks
open EventStore.Client


module DomainEvent =

    let get (client: EventStoreClient) aggType aggKey version =
        let streamName = aggType + aggKey
        let result = client.ReadStreamAsync (Direction.Forwards, streamName, StreamPosition(version))
        result.Select(fun e -> e.Event.EventNumber.ToUInt64(), e.Event.EventType, e.Event.Data).ToEnumerable()

    let write (client: EventStoreClient) aggType aggKey version eData =
        let streamName = aggType + aggKey
        let eventData = eData |> Seq.map (fun (evType, data, metadata) -> EventData (Uuid.NewUuid(), evType, data, metadata))
        client.AppendToStreamAsync (streamName, StreamRevision version, eventData) |> Async.AwaitTask |> Async.Ignore

    let filter (client: EventStoreClient) filterType handler = async {
        let dropped = TaskCompletionSource<SubscriptionDroppedReason * exn>()
        let filter =
            match filterType with
            | StreamPrefix sp -> StreamFilter.Prefix sp
            | StreamRegular sr -> StreamFilter.RegularExpression sr
            | EventTypePrefix ep -> EventTypeFilter.Prefix ep
            | EventTypeRegular er -> EventTypeFilter.RegularExpression er
        use! sub =
            client.SubscribeToAllAsync (
                (fun sub e ct ->
                    let version = e.Event.EventNumber.ToUInt64()
                    let streamName = e.Event.EventStreamId
                    let len = streamName.Length
                    let idx = streamName.IndexOf '-'
                    let aggKey = streamName.[idx + 1..len - 1]
                    handler aggKey e.Event.EventType version e.Event.Data |> Async.Start
                    Task.CompletedTask),
                false,
                (fun sub r ex -> dropped.SetResult (r, ex)),
                SubscriptionFilterOptions filter
            ) |> Async.AwaitTask
        return! dropped.Task |> Async.AwaitTask }