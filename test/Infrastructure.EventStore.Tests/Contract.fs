namespace Infrastructure.EventStore.Tests

open System
open System.Text.Json


[<CLIMutable>]
type CreateNote =
    { Title: string; Content: string }
    static member FullName = "Note.CreateNote"
    member this.Raw () = JsonSerializer.SerializeToUtf8Bytes this |> ReadOnlyMemory

[<CLIMutable>]
type ChangeNote =
    { Content: string }
    static member FullName = "Note.ChangeNote"
    member this.Raw () = JsonSerializer.SerializeToUtf8Bytes this |> ReadOnlyMemory

[<CLIMutable>]
type Note = { Title: string; Content: string }