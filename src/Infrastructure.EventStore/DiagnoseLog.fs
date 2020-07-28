namespace UniStream.Infrastructure.EventStore

open EventStore.Client


module DiagnoseLog =

    let write (client: EventStoreClient) ctx aggType data = async {
        let eventData = EventData (Uuid.NewUuid(), aggType, data)
        do! client.AppendToStreamAsync (ctx, StreamState.Any, seq { eventData }) |> Async.AwaitTask |> Async.Ignore }