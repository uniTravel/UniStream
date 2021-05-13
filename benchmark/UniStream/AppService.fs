namespace Benchmark.UniStream

open System
open System.Net.Http
open EventStore.Client
open UniStream.Infrastructure.EventStore
open UniStream.Domain


module App =

    let config () =
        let connes = "esdb://localhost:9011?tls=true&tlsVerifyCert=false"
        let conncs = "esdb://localhost:9012?tls=true&tlsVerifyCert=false"
        let ses = EventStoreClientSettings.Create connes
        let scs = EventStoreClientSettings.Create conncs
        let ces = new EventStoreClient (ses)
        let ccs = new EventStoreClient (scs)
        let reader = DomainEvent.get ces
        let writer = DomainEvent.write ces
        reader, writer


[<Sealed>]
type ImmuteService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) =

    let actor : Immutable.T<Actor.T> = Immutable.create <| Config.Immutable writer

    member _.CreateActor aggId traceId cv =
        let command = CreateActor.create cv
        Immutable.apply actor aggId traceId command


[<Sealed>]
type BasicService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) =

    let note : Mutable.T<Note.T> = Mutable.create <| Config.Mutable (reader, writer)

    member _.CreateNote aggId traceId cv =
        let command = CreateNote.create cv
        Mutable.apply note aggId traceId command

    member _.ChangeNote aggId traceId cv =
        let command = ChangeNote.create cv
        Mutable.apply note aggId traceId command


[<Sealed>]
type BatchService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) =

    let note : Mutable.T<Note.T> = Mutable.create <| Config.Mutable (reader, writer, batch = 7u)

    member _.CreateNote aggId traceId cv =
        let command = CreateNote.create cv
        Mutable.apply note aggId traceId command

    member _.ChangeNote aggId traceId cv =
        let command = ChangeNote.create cv
        Mutable.apply note aggId traceId command