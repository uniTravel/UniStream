namespace Benchmark.UniStream

open System
open System.Net.Http
open EventStore.Client
open UniStream.Infrastructure.EventStore
open UniStream.Domain


module App =

    let createHttpMessageHandler () =
        let handler = new HttpClientHandler()
        handler.ServerCertificateCustomValidationCallback <- fun _ _ _ _ -> true
        handler :> HttpMessageHandler

    let config () =
        let ses = EventStoreClientSettings()
        let sld = EventStoreClientSettings()
        let slg = EventStoreClientSettings()
        ses.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
        sld.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
        slg.CreateHttpMessageHandler <- fun () -> createHttpMessageHandler()
        ses.ConnectivitySettings.Address <- Uri "https://localhost:9011"
        sld.ConnectivitySettings.Address <- Uri "https://localhost:9012"
        slg.ConnectivitySettings.Address <- Uri "https://localhost:9013"
        let ces = new EventStoreClient (ses)
        let cld = new EventStoreClient (sld)
        let clg = new EventStoreClient (slg)
        let reader = DomainEvent.get ces
        let writer = DomainEvent.write ces
        let ld = DomainLog.write cld
        let lg = DiagnoseLog.write clg
        reader, writer, ld, lg


[<Sealed>]
type ImmuteService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>,
         ld: string -> string -> string -> ReadOnlyMemory<byte> -> Async<unit>,
         lg: string -> string -> ReadOnlyMemory<byte> -> Async<unit>) =

    let actor : Immutable.T<Actor.T> = Immutable.create <| Config.Immutable (writer, ld "NoteApp", lg "NoteApp")

    member _.CreateActor user aggId traceId cv =
        let command = CreateActor.create cv
        Immutable.apply actor user aggId traceId command


[<Sealed>]
type BasicService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>,
         ld: string -> string -> string -> ReadOnlyMemory<byte> -> Async<unit>,
         lg: string -> string -> ReadOnlyMemory<byte> -> Async<unit>) =

    let note : Mutable.T<Note.T> = Mutable.create <| Config.Mutable (reader, writer, ld "NoteApp", lg "NoteApp")

    member _.CreateNote user aggId traceId cv =
        let command = CreateNote.create cv
        Mutable.apply note user aggId traceId command

    member _.ChangeNote user aggId traceId cv =
        let command = ChangeNote.create cv
        Mutable.apply note user aggId traceId command


[<Sealed>]
type BatchService
        (reader: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>,
         ld: string -> string -> string -> ReadOnlyMemory<byte> -> Async<unit>,
         lg: string -> string -> ReadOnlyMemory<byte> -> Async<unit>) =

    let note : Mutable.T<Note.T> = Mutable.create <| Config.Mutable (reader, writer, ld "NoteApp", lg "NoteApp", ?batch = Some 7u)

    member _.CreateNote user aggId traceId cv =
        let command = CreateNote.create cv
        Mutable.apply note user aggId traceId command

    member _.ChangeNote user aggId traceId cv =
        let command = ChangeNote.create cv
        Mutable.apply note user aggId traceId command