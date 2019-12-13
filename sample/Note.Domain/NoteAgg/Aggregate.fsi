namespace Note.Domain.NoteAgg


[<CLIMutable>]
type Create = { Title: string; Content: string }

[<CLIMutable>]
type Change = { Content: string }

module Note =

    type T =
        | Init
        | Active of {| Title: string; Content: string |}

    val inline internal noteCreated : ^c -> T -> T
        when ^c : (member Value: Create)

    val inline internal noteChanged : ^c -> T -> T
        when ^c : (member Value: Change)