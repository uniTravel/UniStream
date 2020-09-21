namespace UniStream.Infrastructure.EventStore

open System
open System.Text
open System.Text.Json
open System.Threading.Tasks
open System.Collections.Generic
open EventStore.Client


module CommandSubscriber =

    type Msg =
        | Sub of string * PersistentSubscription
        | Resub of string * PersistentSubscription
        | Unsub of string

    type T =
        { Client: EventStoreClient
          SubClient: EventStorePersistentSubscriptionsClient
          Subs: Dictionary<string, PersistentSubscription>
          Agent: MailboxProcessor<Msg> }

    let agent (subs: Dictionary<string, PersistentSubscription>) =
        MailboxProcessor<Msg>.Start <| fun inbox ->
            let rec loop () = async {
                match! inbox.Receive() with
                | Sub (group, sub) ->
                    if not <| subs.ContainsKey group then subs.Add (group, sub)
                | Resub (group, sub) ->
                    subs.[group] <- sub
                | Unsub group ->
                    let sub = subs.[group]
                    subs.Remove group |> ignore
                    sub.Dispose()
                return! loop () }
            loop ()

    let create client subClient =
        let subs = Dictionary<string, PersistentSubscription>()
        let agent = agent subs
        { Client = client; SubClient = subClient; Subs = subs; Agent = agent }

    let inline sub< ^c, ^v when ^c : (static member FullName : string)>
        { Client = client; SubClient = subClient; Subs = subs; Agent = agent } group handler = async {
        let cvType = (^c : (static member FullName : string)())
        let rec subscribe resub =
            let sub =
                subClient.SubscribeAsync (cvType, group,
                    (fun sub e r ct ->
                        let traceId = e.Event.EventId.ToString()
                        let user = e.Event.EventType
                        let metadata = e.Event.Metadata.ToArray() |> Encoding.ASCII.GetString
                        let correlationId = metadata.[19..metadata.Length - 3]
                        let callback (v: ^v) =
                            let data = JsonSerializer.SerializeToUtf8Bytes v |> ReadOnlyMemory
                            let eventData = EventData (Uuid.NewUuid(), cvType, data)
                            client.AppendToStreamAsync (traceId, StreamState.NoStream, seq { eventData })
                            |> Async.AwaitTask |> Async.Ignore |> Async.Start
                        match handler cvType traceId user correlationId e.Event.Data callback |> Async.Catch |> Async.RunSynchronously with
                        | Choice1Of2 () -> Task.CompletedTask
                        | Choice2Of2 ex -> Task.FromException ex),
                    (fun sub r ex ->
                        match r with
                        | SubscriptionDroppedReason.Disposed -> ()
                        | _ -> subscribe true)
                ) |> Async.AwaitTask |> Async.RunSynchronously
            if resub then agent.Post <| Resub (group, sub)
            else agent.Post <| Sub (group, sub)
        if not <| subs.ContainsKey group then subscribe false }

    let unsub { Subs = subs; Agent = agent } group = async {
        if subs.ContainsKey group then agent.Post <| Unsub group }