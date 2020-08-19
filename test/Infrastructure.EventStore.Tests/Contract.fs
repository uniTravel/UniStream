namespace Infrastructure.EventStore.Tests

open System
open System.Text.Json


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