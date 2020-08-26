namespace Note

open System
open Note.Contract


[<RequireQualifiedAccess>]
module CreateActor =
    [<Sealed>]
    type T =
        static member FullName : string
        member Apply : (Actor.T -> (string * ReadOnlyMemory<byte>) seq * Actor.T)
    val create : (CreateActor -> T)


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