[<AutoOpen>]
module Note.Tests.ApiConfig

open System
open System.Net.Http
open EventStore.Client
open UniStream.Infrastructure.EventStore
open UniStream.Domain
open Note
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

[<Sealed>]
type NoteService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) =

    let actor = Immutable.create <| Config.Immutable writer
    let note1 = Mutable.create <| Config.Mutable (reader, writer)
    let note2 = Mutable.create <| Config.Mutable (reader, writer, ?batch = Some 7u)
    let obs = Observer.create <| Config.Observer reader

    member _.CreateActor aggKey traceId cmd =
        CommandService.createActor actor aggKey traceId cmd

    member _.CreateNote aggKey traceId cmd =
        CommandService.createNote note1 aggKey traceId cmd

    member _.ChangeNote aggKey traceId cmd =
        CommandService.changeNote note1 aggKey traceId cmd

    member _.BatchCreate aggKey traceId cmd =
        CommandService.createNote note2 aggKey traceId cmd

    member _.BatchChange aggKey traceId cmd =
        CommandService.changeNote note2 aggKey traceId cmd

    member _.AppendNote aggKey number evType data =
        CommandService.appendNote obs aggKey number evType data

let app = NoteService (reader, writer)