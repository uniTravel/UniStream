namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        static member DeltaType : string
        member Value : CreateNote
        member ApplyEvent : (Note.T -> Note.T)

    val create : (CreateNote -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        static member DeltaType : string
        member Value : ChangeNote
        member ApplyEvent : (Note.T -> Note.T)

    val create : (ChangeNote -> T)