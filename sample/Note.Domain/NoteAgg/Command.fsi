namespace Note.Domain.NoteAgg


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        member Value : NoteCreated
        member ApplyEvent : (Note.T -> Note.T)

    val create : (NoteCreated -> T)

[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        member Value : NoteChanged
        member ApplyEvent : (Note.T -> Note.T)

    val create : (NoteChanged -> T)