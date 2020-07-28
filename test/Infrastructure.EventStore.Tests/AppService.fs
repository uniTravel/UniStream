namespace Infrastructure.EventStore.Tests

open System
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

    member _.Filter = DomainEvent.filter es

    member _.Domain = DomainLog.write ld

    member _.Diagnose = DiagnoseLog.write lg

    member _.LaunchCreateNote = DomainCommand.launch<CreateNote, Note> cs

    member _.SubscribeCreateNote = DomainCommand.subscribe<CreateNote, Note> cs ps


module EventStoreConfig =

    let app =
        let ses = EventStoreClientSettings()
        let scs = EventStoreClientSettings()
        let sld = EventStoreClientSettings()
        let slg = EventStoreClientSettings()
        scs.DefaultCredentials <- UserCredentials ("admin", "changeit")
        AppService (ses, scs, sld, slg)