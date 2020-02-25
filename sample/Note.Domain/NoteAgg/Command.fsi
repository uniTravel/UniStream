namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Value : CreateNote
        member Apply : (Note.T -> (string * byte[])[] * Note.T)

    val create : (CreateNote -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Value : ChangeNote
        member Apply : (Note.T -> (string * byte[])[] * Note.T)

    val create : (ChangeNote -> T)