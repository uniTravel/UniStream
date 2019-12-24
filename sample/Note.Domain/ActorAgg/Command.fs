namespace Note.Domain.ActorAgg

open UniStream.Domain


module CreateActor =
    type T = CreateActor of ActorCreated with
        member this.Value = let (CreateActor c) = this in c
        member this.ApplyEvent = Actor.actorCreated this.Value
    let isValid _ = true
    let create = Command.create isValid CreateActor