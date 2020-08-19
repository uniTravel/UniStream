namespace Note.Domain

open UniStream.Domain


module CreateNote =
    type T = CreateNote of CreateNoteCommand with
        static member FullName = typeof<CreateNoteCommand>.FullName
        member this.Apply = Note.createNote <| let (CreateNote c) = this in c
    let private isValid _ = true
    let create = Command.create isValid CreateNote


module ChangeNote =
    type T = ChangeNote of ChangeNoteCommand with
        static member FullName = typeof<ChangeNoteCommand>.FullName
        member this.Apply = Note.changeNote <| let (ChangeNote c) = this in c
    let private isValid _ = true
    let create = Command.create isValid ChangeNote