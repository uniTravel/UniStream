namespace Note.Domain.NoteAgg

open UniStream.Abstract


[<CLIMutable>]
type CreateNote = {
    Title: string
    Content: string
} with interface IValue

[<CLIMutable>]
type ChangeNote = {
    Content: string
} with interface IValue