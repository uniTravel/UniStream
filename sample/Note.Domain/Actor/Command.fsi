namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member DeltaType : string
        member Value : CreateActor
        member ApplyEvent : (Actor.T -> Actor.T)

    val create : (CreateActor -> T)