namespace Note.Domain.ActorAgg

open UniStream.Domain


module CreateActor =
    type T = CreateActor of Create with
        member this.Value = let (CreateActor c) = this in c
        member this.Apply = Actor.actorCreated this
    let isValid _ = true
    let create = Command.create isValid CreateActor