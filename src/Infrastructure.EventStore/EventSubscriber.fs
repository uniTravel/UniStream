namespace UniStream.Infrastructure.EventStore

open System
open System.Threading.Tasks
open System.Collections.Generic
open EventStore.Client


module EventSubscriber =

    type Msg =
        | Sub of string * StreamSubscription
        | Resub of string * StreamSubscription
        | Unsub of string

    type T =
        { Client: EventStoreClient
          AggType: string
          Subs: Dictionary<string, StreamSubscription>
          Handler: string -> string -> uint64 -> ReadOnlyMemory<byte> -> Async<unit>
          Agent: MailboxProcessor<Msg> }

    let agent (subs: Dictionary<string, StreamSubscription>) =
        MailboxProcessor<Msg>.Start <| fun inbox ->
            let rec loop () = async {
                match! inbox.Receive() with
                | Sub (aggKey, sub) ->
                    if not <| subs.ContainsKey aggKey then subs.Add (aggKey, sub)
                | Resub (aggKey, sub) ->
                    subs.[aggKey] <- sub
                | Unsub aggKey ->
                    let sub = subs.[aggKey]
                    subs.Remove aggKey |> ignore
                    sub.Dispose()
                return! loop () }
            loop ()

    let create client aggType handler =
        let aggType = aggType + "-"
        let subs = Dictionary<string, StreamSubscription>()
        let agent = agent subs
        { Client = client; AggType = aggType; Subs = subs; Handler = handler; Agent = agent }

    let sub { Client = client; AggType = aggType; Subs = subs; Handler = handler; Agent = agent } aggKey position = async {
        let stream = aggType + aggKey
        let checkpoint = ref position
        let rec subscribe resub =
            let sub =
                client.SubscribeToStreamAsync (
                    stream,
                    !checkpoint,
                    (fun sub (e: ResolvedEvent) ct ->
                        let version = e.Event.EventNumber.ToUInt64()
                        match handler aggKey e.Event.EventType version e.Event.Data |> Async.Catch |> Async.RunSynchronously with
                        | Choice1Of2 () -> checkpoint := e.OriginalEventNumber; Task.CompletedTask
                        | Choice2Of2 ex -> Task.FromException ex),
                    false,
                    (fun sub r ex ->
                        match r with
                        | SubscriptionDroppedReason.Disposed -> ()
                        | _ -> subscribe true)
                ) |> Async.AwaitTask |> Async.RunSynchronously
            if resub then agent.Post <| Resub (aggKey, sub)
            else agent.Post <| Sub (aggKey, sub)
        if not <| subs.ContainsKey stream then subscribe false }

    let unsub { Subs = subs; Agent = agent } aggKey = async {
        if subs.ContainsKey aggKey then agent.Post <| Unsub aggKey }