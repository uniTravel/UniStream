namespace Benchmark.UniStream

open UniStream.Domain


[<CLIMutable>]
type CreateNote = { Title: string; Content: string }

[<CLIMutable>]
type ChangeNote = { Content: string }


module CreateNote =
    type T = CreateNote of CreateNote with
        static member FullName = typeof<CreateNote>.FullName
        member this.Raw () = Delta.serialize <| let (CreateNote v) = this in v
    let private isValid _ = true
    let create = Command.create isValid CreateNote

module ChangeNote =
    type T = ChangeNote of ChangeNote with
        static member FullName = typeof<ChangeNote>.FullName
        member this.Raw () = Delta.serialize  <| let (ChangeNote v) = this in v
    let private isValid _ = true
    let create = Command.create isValid ChangeNote