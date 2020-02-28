namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Apply : (Note.T -> (string * byte[])[] * Note.T)

    val create : (CreateNote -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Apply : (Note.T -> (string * byte[])[] * Note.T)

    val create : (ChangeNote -> T)