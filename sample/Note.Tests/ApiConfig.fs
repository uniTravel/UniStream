[<AutoOpen>]
module Note.Tests.ApiConfig

open System
open System.Net.Http
open EventStore.Client
open UniStream.Infrastructure.EventStore
open UniStream.Domain
open Note.Domain
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

[<Sealed>]
type NoteService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>,
         ld: string -> string -> string -> ReadOnlyMemory<byte> -> Async<unit>,
         lg: string -> string -> ReadOnlyMemory<byte> -> Async<unit>) =

    let actor = Immutable.create <| Config.Immutable (writer, ld "NoteApp", lg "NoteApp")
    let note1 = Mutable.create <| Config.Mutable (reader, writer, ld "NoteApp", lg "NoteApp")
    let note2 = Mutable.create <| Config.Mutable (reader, writer, ld "NoteApp", lg "NoteApp", ?batch = Some 7u)
    let obs = Observer.create <| Config.Observer (reader, lg "NoteApp")

    member _.CreateActor user aggKey traceId cmd =
        CommandService.createActor actor user aggKey traceId cmd

    member _.CreateNote user aggKey traceId cmd =
        CommandService.createNote note1 user aggKey traceId cmd

    member _.ChangeNote user aggKey traceId cmd =
        CommandService.changeNote note1 user aggKey traceId cmd

    member _.BatchCreate user aggKey traceId cmd =
        CommandService.createNote note2 user aggKey traceId cmd

    member _.BatchChange user aggKey traceId cmd =
        CommandService.changeNote note2 user aggKey traceId cmd

    member _.AppendNote aggKey number evType data =
        CommandService.appendNote obs aggKey number evType data

let app = NoteService (reader, writer, ld, lg)