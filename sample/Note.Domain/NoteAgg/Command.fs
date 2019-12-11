namespace Note.Domain.NoteAgg

open UniStream.Domain


module CreateNote =
    type T = CreateNote of Create with
        member this.Value = let (CreateNote c) = this in c
        member this.Apply = Note.noteCreated this
    let isValid _ = true
    let create = Command.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of Change with
        member this.Value = let (ChangeNote c) = this in c
        member this.Apply = Note.noteChanged this
    let isValid _ = true
    let create = Command.create isValid ChangeNote