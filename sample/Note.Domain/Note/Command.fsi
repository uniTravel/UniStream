namespace Note.Domain


[<CLIMutable>]
type CreateNote = { Title: string; Content: string }

[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Apply : (Note.T -> string -> (string * byte[] * byte[]) seq * Note.T)

    val create : (CreateNote -> T)

[<CLIMutable>]
type ChangeNote = { Content: string }

[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        static member ValueType : string
        member Apply : (Note.T -> string -> (string * byte[] * byte[]) seq * Note.T)

    val create : (ChangeNote -> T)