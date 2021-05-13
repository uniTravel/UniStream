namespace UniStream.Infrastructure.EventStore

open System.Threading.Tasks
open System.Collections.Generic
open EventStore.Client


module EventFilter =

    type Msg =
        | Sub of SubscriptionFilterOptions * StreamSubscription
        | Resub of SubscriptionFilterOptions * StreamSubscription
        | Unsub of SubscriptionFilterOptions

    type T =
        { Client: EventStoreClient
          Subs: Dictionary<SubscriptionFilterOptions, StreamSubscription>
          Agent: MailboxProcessor<Msg> }

    let agent (subs: Dictionary<SubscriptionFilterOptions, StreamSubscription>) =
        MailboxProcessor<Msg>.Start <| fun inbox ->
            let rec loop () = async {
                match! inbox.Receive() with
                | Sub (filter, sub) ->
                    if not <| subs.ContainsKey filter then subs.Add (filter, sub)
                | Resub (filter, sub) ->
                    subs.[filter] <- sub
                | Unsub filter ->
                    let sub = subs.[filter]
                    subs.Remove filter |> ignore
                    sub.Dispose()
                return! loop () }
            loop ()

    let create client =
        let subs = Dictionary<SubscriptionFilterOptions, StreamSubscription>()
        let agent = agent subs
        { Client = client; Subs = subs; Agent = agent }

    let sub { Client = client; Subs = subs; Agent = agent } filter position handler = async {
        let position = ref position
        let rec subscribe resub =
            let sub =
                client.SubscribeToAllAsync (
                    !position,
                    (fun sub (e: ResolvedEvent) ct ->
                        let version = e.Event.EventNumber.ToUInt64()
                        let streamName = e.Event.EventStreamId
                        let idx = streamName.IndexOf '-'
                        let aggKey = streamName.[idx + 1..]
                        match handler aggKey version e.Event.EventType e.Event.Data |> Async.Catch |> Async.RunSynchronously with
                        | Choice1Of2 () -> position := e.OriginalPosition.Value; Task.CompletedTask
                        | Choice2Of2 ex -> Task.FromException ex),
                    false,
                    (fun sub r ex ->
                        match r with
                        | SubscriptionDroppedReason.Disposed -> ()
                        | _ -> subscribe true ),
                    filter
                ) |> Async.AwaitTask |> Async.RunSynchronously
            if resub then agent.Post <| Resub (filter, sub)
            else agent.Post <| Sub (filter, sub)
        if not <| subs.ContainsKey filter then subscribe false }

    let unsub  { Subs = subs; Agent = agent } filter = async {
        if subs.ContainsKey filter then agent.Post <| Unsub filter }