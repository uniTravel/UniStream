namespace Note.Domain

open UniStream.Domain


module CreateActor =
    type T = CreateActor of ActorCreated with
        static member DeltaType = typeof<ActorCreated>.FullName
        member this.Value = let (CreateActor c) = this in c
        member this.ApplyEvent = Actor.actorCreated this.Value
    let isValid _ = true
    let create = Command.create isValid CreateActor