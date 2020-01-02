namespace Note.Domain.NoteAgg

open UniStream.Domain


module CreateNote =
    type T = CreateNote of NoteCreated with
        static member DeltaType = typeof<NoteCreated>.FullName
        member this.Value = let (CreateNote c) = this in c
        member this.ApplyEvent = Note.noteCreated this.Value
    let isValid _ = true
    let create = Command.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of NoteChanged with
        static member DeltaType = typeof<NoteChanged>.FullName
        member this.Value = let (ChangeNote c) = this in c
        member this.ApplyEvent = Note.noteChanged this.Value
    let isValid _ = true
    let create = Command.create isValid ChangeNote