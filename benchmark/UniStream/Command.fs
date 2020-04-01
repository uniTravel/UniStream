namespace Benchmark.UniStream

open UniStream.Domain


[<CLIMutable>]
type CreateNote = { Title: string; Content: string }

[<RequireQualifiedAccess>]
module CreateNote =

    type T = CreateNote of CreateNote with
        static member ValueType = typeof<CreateNote>.FullName
        member this.Apply =
            let cv = let (CreateNote c) = this in c
            Note.createNote { Title = cv.Title; Content = cv.Content }
    let private isValid _ = true
    let create = Command.create isValid CreateNote


[<CLIMutable>]
type ChangeNote = { Content: string }

[<RequireQualifiedAccess>]
module ChangeNote =
    type T = ChangeNote of ChangeNote with
        static member ValueType = typeof<ChangeNote>.FullName
        member this.Value = let (ChangeNote c) = this in c
        member this.Apply =
            let cv = let (ChangeNote c) = this in c
            Note.changeNote { Content = cv.Content }
    let private isValid _ = true
    let create = Command.create isValid ChangeNote