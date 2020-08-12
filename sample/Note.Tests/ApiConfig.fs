[<AutoOpen>]
module ApiConfig

open System
open System.Net.Http
open EventStore.Client
open UniStream.Infrastructure.EventStore
open Note.Application


let createHttpMessageHandler () =
    let handler = new HttpClientHandler()
    handler.ServerCertificateCustomValidationCallback <- fun _ _ _ _ -> true
    handler :> HttpMessageHandler

let ses = EventStoreClientSettings()
let scs = EventStoreClientSettings()
let sld = EventStoreClientSettings()
let slg = EventStoreClientSettings()
scs.DefaultCredentials <- UserCredentials ("admin", "changeit")
ses.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
scs.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
sld.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
slg.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
ses.ConnectivitySettings.Address <- Uri "https://localhost:9011"
scs.ConnectivitySettings.Address <- Uri "https://localhost:9016"
sld.ConnectivitySettings.Address <- Uri "https://localhost:9012"
slg.ConnectivitySettings.Address <- Uri "https://localhost:9013"
let ces = new EventStoreClient (ses)
let ccs = new EventStoreClient (scs)
let cld = new EventStoreClient (sld)
let clg = new EventStoreClient (slg)

let reader = DomainEvent.get ces
let writer = DomainEvent.write ces
let ld = DomainLog.write cld
let lg = DiagnoseLog.write clg

let app = NoteService (reader, writer, ld, lg)