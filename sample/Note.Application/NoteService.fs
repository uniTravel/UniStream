namespace Note.Application

open System
open UniStream.Domain


[<Sealed>]
type NoteService
        (reader: string -> string -> uint64 -> (uint64 * string * ReadOnlyMemory<byte>) seq,
         writer: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>,
         ld: string -> string -> string -> ReadOnlyMemory<byte> -> Async<unit>,
         lg: string -> string -> ReadOnlyMemory<byte> -> Async<unit>) =

    let actor = Immutable.create <| Config.Immutable (writer, ld "NoteApp", lg "NoteApp")
    let note = Mutable.create <| Config.Mutable (reader, writer, ld "NoteApp", lg "NoteApp")
    let obs = Observer.create <| Config.Observer (reader, lg "NoteApp")

    member _.CreateActor user aggId traceId cv =
        CommandService.createActor actor user aggId traceId cv

    member _.CreateNote user aggId traceId cv =
        CommandService.createNote note user aggId traceId cv

    member _.ChangeNote user aggId traceId cv =
        CommandService.changeNote note user aggId traceId cv

    member _.AppendNote aggId number evType data =
        CommandService.appendNote obs aggId number evType data