namespace Note.Domain


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member DeltaType : string
        member Value : ActorCreated
        member ApplyEvent : (Actor.T -> Actor.T)

    val create : (ActorCreated -> T)