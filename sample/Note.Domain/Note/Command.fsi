namespace Note.Domain

open System
open Note.Contract


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        static member FullName : string
        member Apply : (Note.T -> (string * ReadOnlyMemory<byte>) seq * Note.T)

    val create : (CreateNote -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        static member FullName : string
        member Apply : (Note.T -> (string * ReadOnlyMemory<byte>) seq * Note.T)

    val create : (ChangeNote -> T)