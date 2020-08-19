namespace Note.Domain

open System


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        static member FullName : string
        member Apply : (Note.T -> (string * ReadOnlyMemory<byte>) seq * Note.T)

    val create : (CreateNoteCommand -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        static member FullName : string
        member Apply : (Note.T -> (string * ReadOnlyMemory<byte>) seq * Note.T)

    val create : (ChangeNoteCommand -> T)