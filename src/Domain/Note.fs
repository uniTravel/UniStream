namespace UniStream.Domain.Note

open System
open UniStream.Domain

[<CLIMutable>]
type Note = {
    Id: Guid
    Title: string
} with interface IValue

module Note =

    type Active = { Title: string }
    type T =
        | Init
        | Active of Active
        with interface IAggregate

    let noteCreated event t =
        match t with
        | Init ->
            let n = (event :> IWrapped<Note>).Value
            let agg = Active { Title = n.Title }
            agg, 0, n
        | Active _ -> failwith ""

module NoteCreated =
    type T = NoteCreated of Note with
        interface IWrapped<Note> with
            member this.Value = let (NoteCreated e) = this in e
        interface IDomainEvent<Note, Note.T> with
            member this.Apply = Note.noteCreated this
    let create = DomainEvent.create NoteCreated
    let convert c = DomainEvent.apply create c

module NoteChanged =
    type T = NoteChanged of Note with
        interface IWrapped<Note> with
            member this.Value = let (NoteChanged e) = this in e
        interface IDomainEvent<Note, Note.T> with
            member this.Apply = failwith ""
    let create = DomainEvent.create NoteChanged
    let convert c = DomainEvent.apply create c

module CreateNote =
    type T = CreateNote of Note with
        interface IWrapped<Note> with
            member this.Value = let (CreateNote c) = this in c
        interface IDomainCommand<Note, Note.T, NoteCreated.T> with
            member this.Convert = NoteCreated.convert this
    let isValid c = true
    let create = DomainCommand.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of Note with
        interface IWrapped<Note> with
            member this.Value = let (ChangeNote c) = this in c
        interface IDomainCommand<Note, Note.T, NoteChanged.T> with
            member this.Convert = NoteChanged.convert this
    let isValid _ = true
    let create = DomainCommand.create isValid ChangeNote


module Test =
    let cn = CreateNote.create { Id = System.Guid.NewGuid(); Title = "test" }
    let e = NoteChanged.convert cn
