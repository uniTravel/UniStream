namespace Infrastructure.EventStore.Tests

open System
open System.Linq
open System.Text
open EventStore.Client
open UniStream.Infrastructure.EventStore


type AppService (ses: EventStoreClientSettings, scs: EventStoreClientSettings) =

    let es = new EventStoreClient (ses)
    let cs = new EventStoreClient (scs)
    let ps = new EventStorePersistentSubscriptionsClient(scs)

    do
        cs.SoftDeleteAsync ("Note.CreateNote", StreamState.Any) |> Async.AwaitTask |> Async.RunSynchronously |> ignore
        cs.SoftDeleteAsync ("Note.ChangeNote", StreamState.Any) |> Async.AwaitTask |> Async.RunSynchronously |> ignore

    member _.Reader = DomainEvent.get es

    member _.Writer = DomainEvent.write es

    member _.EventSubscriber = EventSubscriber.create es

    member _.EventFilter = EventFilter.create es

    member _.Position () =
        let r = es.ReadAllAsync(Direction.Backwards, Position.End, 1L).ToEnumerable().FirstOrDefault()
        r.OriginalPosition.Value

    member _.CommandSubscriber = CommandSubscriber.create cs ps

    member _.CreateNote = DomainCommand.launch<CreateNote, Note> cs 300


[<AutoOpen>]
module EventStoreConfig =

    let app =
        let ces = "esdb://admin:changeit@localhost:9011?tls=true&tlsVerifyCert=false"
        let ccs = "esdb://admin:changeit@localhost:9012?tls=true&tlsVerifyCert=false"
        let ses = EventStoreClientSettings.Create ces
        let scs = EventStoreClientSettings.Create ccs
        AppService (ses, scs)

    let createEvents count =
        seq { for i in 1 .. count ->
                let e = Encoding.UTF8.GetBytes ("test" + i.ToString()) |> ReadOnlyMemory
                "Changed", e, Nullable() }