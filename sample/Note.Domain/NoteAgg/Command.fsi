namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        member Value : CreateNote
        member Apply : (Note.T -> (string * byte[])[] * Note.T)

    val create : (CreateNote -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        member Value : ChangeNote
        member Apply : (Note.T -> (string * byte[])[] * Note.T)

    val create : (ChangeNote -> T)