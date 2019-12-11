namespace Note.Domain.NoteAgg


[<CLIMutable>]
type Create = {
    Title: string
    Content: string
}

[<CLIMutable>]
type Change = {
    Content: string
}