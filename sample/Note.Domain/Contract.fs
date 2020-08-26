namespace Note.Contract


[<CLIMutable>]
type CreateActor = { Name: string }

[<CLIMutable>]
type CreateNote = { Title: string; Content: string }

[<CLIMutable>]
type ChangeNote = { Content: string }