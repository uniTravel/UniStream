namespace Benchmark.UniStream

open UniStream.Domain


[<CLIMutable>]
type NoteCreated = { Title: string; Content: string }

[<CLIMutable>]
type NoteChanged = { Content: string }

[<RequireQualifiedAccess>]
module Note =

    type Value =
        { Title: string; Content: string }

    let noteCreated = typeof<NoteCreated>.FullName
    let noteChanged = typeof<NoteChanged>.FullName

    type T =
        | Init
        | Active of Value

    let applyNoteCreated t (ev: NoteCreated) =
        match t with
        | Init -> { Title = ev.Title; Content = ev.Content }
        | _ -> failwith "只有初始状态才能创建Note。"

    let applyNoteChanged t (ev: NoteChanged) =
        match t with
        | Active v -> { v with Content = ev.Content }
        | _ -> failwith "只有Active状态才能改变Note。"

    let apply agg evType evBytes =
        match evType with
        | ev when ev = noteCreated ->
            let ev = Delta.fromBytes<NoteCreated> evBytes
            applyNoteCreated agg ev |> Active
        | ev when ev = noteChanged ->
            let ev = Delta.fromBytes<NoteChanged> evBytes
            applyNoteChanged agg ev |> Active
        | _ -> failwithf "领域事件类型错误：%s" evType

    type T with
        static member Initial = Init
        member this.ApplyEvent = apply this
        member this.Value =
            match this with
            | Active v -> v
            | Init -> failwith "初始状态，尚未赋值。"

    let createNote ev agg aggId =
        seq { noteCreated, Delta.asBytes ev, MetaData.correlationId aggId }, Active <| applyNoteCreated agg ev

    let changeNote ev agg aggId =
        seq { noteChanged, Delta.asBytes ev, MetaData.correlationId aggId }, Active <| applyNoteChanged agg ev