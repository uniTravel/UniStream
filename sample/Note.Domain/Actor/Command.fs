namespace Note.Domain

open UniStream.Domain


module CreateActor =
    type T = CreateActor of CreateActorCommand with
        static member FullName = typeof<CreateActorCommand>.FullName
        member this.Apply = Actor.createActor <| let (CreateActor c) = this in c

    let private isValid _ = true

    let create = Command.create isValid CreateActor