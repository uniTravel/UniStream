namespace Note.Domain

open UniStream.Domain


type CreateActor = { Name: string }


module CreateActor =
    type T = CreateActor of CreateActor with
        static member ValueType = typeof<CreateActor>.FullName
        member this.Apply =
            let cv = let (CreateActor c) = this in c
            Actor.createActor { Name = cv.Name }
    let isValid _ = true
    let create = Command.create isValid CreateActor