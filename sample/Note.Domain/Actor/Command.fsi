namespace Note.Domain

open System


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member FullName : string
        member Apply : (Actor.T -> (string * ReadOnlyMemory<byte>) seq * Actor.T)

    val create : (CreateActorCommand -> T)