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
    let note1 = Mutable.create <| Config.Mutable (reader, writer, ld "NoteApp", lg "NoteApp")
    let note2 = Mutable.create <| Config.Mutable (reader, writer, ld "NoteApp", lg "NoteApp", ?batch = Some 7u)
    let obs = Observer.create <| Config.Observer (reader, lg "NoteApp")

    member _.CreateActor user aggKey traceId cv =
        CommandService.createActor actor user aggKey traceId cv

    member _.CreateNote user aggKey traceId cv =
        CommandService.createNote note1 user aggKey traceId cv

    member _.ChangeNote user aggKey traceId cv =
        CommandService.changeNote note1 user aggKey traceId cv

    member _.BatchCreate user aggKey traceId cv =
        CommandService.createNote note2 user aggKey traceId cv

    member _.BatchChange user aggKey traceId cv =
        CommandService.changeNote note2 user aggKey traceId cv

    member _.AppendNote aggKey number evType data =
        CommandService.appendNote obs aggKey number evType data