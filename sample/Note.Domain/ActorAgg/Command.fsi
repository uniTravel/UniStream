namespace Note.Domain.ActorAgg


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        member Value : ActorCreated
        member ApplyEvent : (Actor.T -> Actor.T)

    val create : (ActorCreated -> T)