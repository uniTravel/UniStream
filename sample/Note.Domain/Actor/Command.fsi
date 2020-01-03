namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateActor =

    [<Sealed>]
    type T =
        static member DeltaType : string
        member Value : CreateActorCommand
        member ApplyEvent : (Actor.T -> Actor.T)

    val create : (CreateActorCommand -> T)