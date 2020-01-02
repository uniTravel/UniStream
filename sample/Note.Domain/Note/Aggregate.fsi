namespace Note.Domain


[<CLIMutable>]
type NoteCreated = { Title: string; Content: string }

[<CLIMutable>]
type NoteChanged = { Content: string }

module Note =

    type T

    val internal noteCreated : NoteCreated -> T -> T

    val internal noteChanged : NoteChanged -> T -> T

    type T with
        static member Empty : T
        member Apply : (string -> byte[] -> T)