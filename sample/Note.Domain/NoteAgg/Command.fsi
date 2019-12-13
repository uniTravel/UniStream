namespace Note.Domain.NoteAgg


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        member Value : Create
        member Apply : (Note.T -> Note.T)

    val create : (Create -> T)

[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        member Value : Change
        member Apply : (Note.T -> Note.T)

    val create : (Change -> T)