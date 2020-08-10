namespace UniStream.Infrastructure.EventStore

open System
open System.Threading.Tasks
open System.Text
open System.Text.Json
open EventStore.Client


module DomainCommand =

    let inline launch< ^c, ^v when ^c : (static member FullName : string)>
        (client: EventStoreClient) correlationId (cmd: ^c) = async {
        let result = TaskCompletionSource<Result< ^v, string>>()
        let cvType = (^c : (static member FullName : string)())
        let traceId = Uuid.NewUuid()
        let data = JsonSerializer.SerializeToUtf8Bytes cmd |> ReadOnlyMemory
        let metadata = Encoding.ASCII.GetBytes ("{\"$correlationId\":\"" + correlationId + "\"}") |> ReadOnlyMemory |> Nullable
        let eventData = EventData (traceId, correlationId, data, metadata)
        use! sub =
            client.SubscribeToStreamAsync (traceId.ToString(),
                (fun sub e ct ->
                    result.TrySetResult <| Ok (JsonSerializer.Deserialize< ^v> e.Event.Data.Span) |> ignore
                    Task.CompletedTask),
                false,
                (fun sub r ex ->
                    let reason = Enum.GetName (typeof<SubscriptionDroppedReason>, r)
                    result.TrySetResult <| Error reason |> ignore)
            ) |> Async.AwaitTask
        do! client.AppendToStreamAsync (cvType, StreamState.Any, seq { eventData }) |> Async.AwaitTask |> Async.Ignore
        return! result.Task |> Async.AwaitTask }

    let inline subscribe< ^c, ^v when ^c : (static member FullName : string)>
        (client: EventStoreClient) (subClient: EventStorePersistentSubscriptionsClient) handler = async {
        let dropped = TaskCompletionSource<SubscriptionDroppedReason * exn>()
        let cvType = (^c : (static member FullName : string)())
        use! sub =
            subClient.SubscribeAsync (cvType, cvType,
                (fun sub e r ct ->
                    let traceId = e.Event.EventId.ToString()
                    let callback (v: ^v) =
                        let data = JsonSerializer.SerializeToUtf8Bytes v |> ReadOnlyMemory
                        let eventData = EventData (Uuid.NewUuid(), cvType, data)
                        client.AppendToStreamAsync (traceId, StreamState.NoStream, seq { eventData })
                        |> Async.AwaitTask |> Async.Ignore |> Async.Start
                    handler traceId e.Event.EventType e.Event.Data callback |> Async.Start
                    Task.CompletedTask),
                (fun sub r ex -> dropped.SetResult (r, ex))
            ) |> Async.AwaitTask
        return! dropped.Task |> Async.AwaitTask }