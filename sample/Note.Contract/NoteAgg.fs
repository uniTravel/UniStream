namespace Note.Domain.NoteAgg

open UniStream.Domain

// module T =
//     let value<'v when 'v :> IValue> =
//         typeof<'v>.FullName, DomainEvent.fromBytes<'v>

[<CLIMutable>]
type CreateNote = {
    Title: string
    Content: string
} with
    interface IValue
    // static member Value = T.value<CreateNote>

[<CLIMutable>]
type ChangeNote = {
    Content: string
} with
    interface IValue