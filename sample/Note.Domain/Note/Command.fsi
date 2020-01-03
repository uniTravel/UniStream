namespace Note.Domain

open Note.Contract


[<RequireQualifiedAccess>]
module CreateNote =

    [<Sealed>]
    type T =
        static member DeltaType : string
        member Value : CreateNoteCommand
        member ApplyEvent : (Note.T -> Note.T)

    val create : (CreateNoteCommand -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    [<Sealed>]
    type T =
        static member DeltaType : string
        member Value : ChangeNoteCommand
        member ApplyEvent : (Note.T -> Note.T)

    val create : (ChangeNoteCommand -> T)