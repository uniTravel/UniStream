namespace UniStream.Infrastructure.EventStore

open EventStore.Client


module DomainLog =

    let write (client: EventStoreClient) ctx user category data = async {
        let streamName = ctx + user
        let eventData = EventData (Uuid.NewUuid(), category, data)
        do! client.AppendToStreamAsync (streamName, StreamState.Any, seq { eventData }) |> Async.AwaitTask |> Async.Ignore }