namespace Infrastructure.EventStore.Tests

open System
open System.Linq
open System.Text
open System.Net.Http
open EventStore.Client
open UniStream.Infrastructure.EventStore


type AppService (ses: EventStoreClientSettings, scs: EventStoreClientSettings, sld: EventStoreClientSettings, slg: EventStoreClientSettings) =

    let createHttpMessageHandler () =
        let handler = new HttpClientHandler()
        handler.ServerCertificateCustomValidationCallback <- fun _ _ _ _ -> true
        handler :> HttpMessageHandler

    do
        ses.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
        scs.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
        sld.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
        slg.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
        ses.ConnectivitySettings.Address <- Uri "https://localhost:9011"
        scs.ConnectivitySettings.Address <- Uri "https://localhost:9016"
        sld.ConnectivitySettings.Address <- Uri "https://localhost:9012"
        slg.ConnectivitySettings.Address <- Uri "https://localhost:9013"

    let es = new EventStoreClient (ses)
    let cs = new EventStoreClient (scs)
    let ps = new EventStorePersistentSubscriptionsClient(scs)
    let ld = new EventStoreClient (sld)
    let lg = new EventStoreClient (slg)

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
        let ses = EventStoreClientSettings()
        let scs = EventStoreClientSettings()
        let sld = EventStoreClientSettings()
        let slg = EventStoreClientSettings()
        ses.DefaultCredentials <- UserCredentials ("admin", "changeit")
        scs.DefaultCredentials <- UserCredentials ("admin", "changeit")
        AppService (ses, scs, sld, slg)

    let createEvents count =
        seq { for i in 1 .. count ->
                let e = Encoding.UTF8.GetBytes ("test" + i.ToString()) |> ReadOnlyMemory
                "Changed", e, Nullable() }