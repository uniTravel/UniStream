namespace Note.Domain.NoteAgg

open UniStream.Abstract
open UniStream.Domain


module CreateNote =
    type T = CreateNote of CreateNote with
        interface IWrapped<CreateNote> with
            member this.Value = let (CreateNote c) = this in c
        interface IDomainCommand<CreateNote, Note.T, NoteCreated.T> with
            member this.Convert = NoteCreated.convert this
    let isValid _ = true
    let create = DomainCommand.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of ChangeNote with
        interface IWrapped<ChangeNote> with
            member this.Value = let (ChangeNote c) = this in c
        interface IDomainCommand<ChangeNote, Note.T, NoteChanged.T> with
            member this.Convert = NoteChanged.convert this
    let isValid _ = true
    let create = DomainCommand.create isValid ChangeNote
