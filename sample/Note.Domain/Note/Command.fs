namespace Note.Domain

open UniStream.Domain
open Note.Contract


module CreateNote =
    type T = CreateNote of CreateNoteCommand with
        static member DeltaType = typeof<CreateNoteCommand>.FullName
        member this.Value = let (CreateNote c) = this in c
        member this.ApplyEvent = Note.noteCreated this.Value
    let isValid _ = true
    let create = Command.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of ChangeNoteCommand with
        static member DeltaType = typeof<ChangeNoteCommand>.FullName
        member this.Value = let (ChangeNote c) = this in c
        member this.ApplyEvent = Note.noteChanged this.Value
    let isValid _ = true
    let create = Command.create isValid ChangeNote