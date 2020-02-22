namespace Note.Domain

open UniStream.Domain
open Note.Contract


module CreateNote =
    type T = CreateNote of CreateNote with
        static member DeltaType = typeof<CreateNote>.FullName
        member this.Value = let (CreateNote c) = this in c
        member this.Apply = Note.createNote this.Value
    let isValid _ = true
    let create = Command.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of ChangeNote with
        static member DeltaType = typeof<ChangeNote>.FullName
        member this.Value = let (ChangeNote c) = this in c
        member this.Apply = Note.changeNote this.Value
    let isValid _ = true
    let create = Command.create isValid ChangeNote