namespace Note.Domain.NoteAgg

open UniStream.Domain


[<CLIMutable>]
type CreateNote = {
    Title: string
    Content: string
} with interface IValue

[<CLIMutable>]
type ChangeNote = {
    Content: string
} with interface IValue