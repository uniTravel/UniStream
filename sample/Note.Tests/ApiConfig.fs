[<AutoOpen>]
module Note.Tests.ApiConfig

open EventStore.Client
open UniStream.Infrastructure.EventStore
open UniStream.Domain
open Note
open Note.Application


let connes = "esdb://localhost:9011?tls=true&tlsVerifyCert=false"
let conncs = "esdb://localhost:9012?tls=true&tlsVerifyCert=false"
let ses = EventStoreClientSettings.Create connes
let scs = EventStoreClientSettings.Create conncs
let ces = new EventStoreClient (ses)
let ccs = new EventStoreClient (scs)

let reader = DomainEvent.get ces
let writer = DomainEvent.write ces

[<Sealed>]
type NoteService (reader, writer) =

    let actor = Immutable.create <| Config.Immutable writer
    let note1 = Mutable.create <| Config.Mutable (reader, writer)
    let note2 = Mutable.create <| Config.Mutable (reader, writer, ?batch = Some 7u)

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

let app = NoteService (reader, writer)