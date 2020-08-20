namespace Infrastructure.EventStore.Tests


[<CLIMutable>]
type CreateNote =
    { Title: string; Content: string }
    static member FullName = "Note.CreateNote"

[<CLIMutable>]
type ChangeNote =
    { Content: string }
    static member FullName = "Note.ChangeNote"

[<CLIMutable>]
type Note = { Title: string; Content: string }