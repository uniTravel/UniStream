namespace Note.Domain.NoteAgg

open UniStream.Abstract
open UniStream.Domain


module NoteCreated =
    type T = NoteCreated of CreateNote with
        interface IWrapped<CreateNote> with
            member this.Value = let (NoteCreated e) = this in e
        interface IDomainEvent<CreateNote, Note.T> with
            member this.Apply = Note.noteCreated this
    let create = DomainEvent.create NoteCreated
    let convert c = DomainEvent.apply create c


module NoteChanged =
    type T = NoteChanged of ChangeNote with
        interface IWrapped<ChangeNote> with
            member this.Value = let (NoteChanged e) = this in e
        interface IDomainEvent<ChangeNote, Note.T> with
            member this.Apply = Note.noteChanged this
    let create = DomainEvent.create NoteChanged
    let convert c = DomainEvent.apply create c