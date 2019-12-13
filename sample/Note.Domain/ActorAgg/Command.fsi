namespace Note.Domain.ActorAgg


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        member Value : Create
        member Apply : (Actor.T -> Actor.T)

    val create : (Create -> T)