namespace Note.Contract

open UniStream.Domain


[<CLIMutable>]
type CreateActor = { Name: string }

[<CLIMutable>]
type Actor = { Name: string; Sex: string }

module CreateActor =
    type T = CreateActor of CreateActor with
        static member FullName = typeof<CreateActor>.FullName
        member this.Raw () = Delta.serialize <| let (CreateActor v) = this in v
    let private isValid _ = true
    let create = Command.create isValid CreateActor