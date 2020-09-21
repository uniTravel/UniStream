namespace UniStream.Infrastructure.EventStore

open System
open System.Threading.Tasks
open System.Text
open System.Text.Json
open EventStore.Client


module DomainCommand =

    let inline launch< ^c, ^v when ^c : (static member FullName : string)>
        (client: EventStoreClient) (timeout: int) user correlationId (cmd: ^c) = async {
        let result = TaskCompletionSource< ^v>()
        let cvType = (^c : (static member FullName : string)())
        let traceId = Uuid.NewUuid()
        let data = JsonSerializer.SerializeToUtf8Bytes cmd |> ReadOnlyMemory
        let metadata = Encoding.ASCII.GetBytes ("{\"$correlationId\":\"" + correlationId + "\"}") |> ReadOnlyMemory |> Nullable
        let eventData = EventData (traceId, user, data, metadata)
        use! sub =
            client.SubscribeToStreamAsync (traceId.ToString(),
                (fun sub e ct ->
                    result.TrySetResult <| JsonSerializer.Deserialize< ^v> e.Event.Data.Span |> ignore
                    Task.CompletedTask),
                false,
                (fun sub r ex ->
                    let reason = Enum.GetName (typeof<SubscriptionDroppedReason>, r)
                    result.TrySetException (exn reason) |> ignore)
            ) |> Async.AwaitTask
        do! client.AppendToStreamAsync (cvType, StreamState.Any, seq { eventData }) |> Async.AwaitTask |> Async.Ignore
        let delay = Task.Delay timeout
        let task = Task.WhenAny (result.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
        if task = delay then result.TrySetException (TimeoutException()) |> ignore
        return! result.Task |> Async.AwaitTask }