namespace Note

open UniStream.Domain
open Note.Contract


module CreateActor =
    type T = CreateActor of CreateActor with
        static member FullName = typeof<CreateActor>.FullName
        member this.Apply = Actor.createActor <| let (CreateActor c) = this in c
    let private isValid _ = true
    let create = Command.create isValid CreateActor


module CreateNote =
    type T = CreateNote of CreateNote with
        static member FullName = typeof<CreateNote>.FullName
        member this.Apply = Note.createNote <| let (CreateNote c) = this in c
    let private isValid _ = true
    let create = Command.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of ChangeNote with
        static member FullName = typeof<ChangeNote>.FullName
        member this.Apply = Note.changeNote <| let (ChangeNote c) = this in c
    let private isValid _ = true
    let create = Command.create isValid ChangeNote