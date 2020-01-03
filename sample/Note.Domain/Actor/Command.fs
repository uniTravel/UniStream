namespace Note.Domain

open UniStream.Domain
open Note.Contract


module CreateActor =
    type T = CreateActor of CreateActorCommand with
        static member DeltaType = typeof<CreateActorCommand>.FullName
        member this.Value = let (CreateActor c) = this in c
        member this.ApplyEvent = Actor.actorCreated this.Value
    let isValid _ = true
    let create = Command.create isValid CreateActor