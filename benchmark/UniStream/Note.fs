namespace Benchmark.UniStream

open UniStream.Domain


[<CLIMutable>]
type NoteCreated = { Title: string; Content: string }

[<CLIMutable>]
type NoteChanged = { Content: string }

[<RequireQualifiedAccess>]
module Note =

    type Value =
        { Title: string; Content: string; Count: int }

    let noteCreated = typeof<NoteCreated>.FullName
    let noteChanged = typeof<NoteChanged>.FullName

    let createNote = typeof<CreateNote>.FullName
    let changeNote = typeof<ChangeNote>.FullName

    type T =
        | Init
        | Active of Value
        | Close of Value

    let applyNoteCreated agg (ev: NoteCreated) =
        match agg with
        | Init -> { Title = ev.Title; Content = ev.Content; Count = 0 }
        | _ -> failwith "只有初始状态才能创建Note。"

    let applyNoteChanged agg (ev: NoteChanged) =
        match agg with
        | Active v -> { v with Content = ev.Content; Count = v.Count + 1 }
        | _ -> failwith "只有Active状态才能改变Note。"

    let applyEvent agg evType data =
        match evType with
        | ev when ev = noteCreated ->
            let ev = Delta.deserialize<NoteCreated> data
            applyNoteCreated agg ev |> Active
        | ev when ev = noteChanged ->
            let ev = Delta.deserialize<NoteChanged> data
            applyNoteChanged agg ev |> Active
        | _ -> failwithf "领域事件类型错误：%s" evType

    let applyCommand agg cvType data =
        match cvType with
        | cv when cv = createNote ->
            let cv = Delta.deserialize<CreateNote> data
            let ev = { Title = cv.Title; Content = cv.Content }
            seq { noteCreated, Delta.serialize ev }, Active <| applyNoteCreated agg ev
        | cv when cv = changeNote ->
            let cv = Delta.deserialize<ChangeNote> data
            let ev = { Content = cv.Content }
            seq { noteChanged, Delta.serialize ev }, Active <| applyNoteChanged agg ev
        | _ -> failwithf "领域命令值类型错误：%s" cvType


    type T with
        static member Initial = Init
        member this.ApplyEvent = applyEvent this
        member this.ApplyCommand = applyCommand this
        member this.Value =
            match this with
            | Active v | Close v -> v
            | Init -> failwith "初始状态，尚未赋值。"
        member this.Closed =
            match this with
            | Close _ -> true
            | _ -> false