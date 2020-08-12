namespace Note.Domain

open System
open Note.Contract


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member FullName : string
        member Apply : (Actor.T -> (string * ReadOnlyMemory<byte>) seq * Actor.T)

    val create : (CreateActor -> T)