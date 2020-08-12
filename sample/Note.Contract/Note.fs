namespace Note.Contract


[<CLIMutable>]
type CreateNote = { Title: string; Content: string }

[<CLIMutable>]
type ChangeNote = { Content: string }

[<CLIMutable>]
type Note = { Title: string; Content: string; Count: int }