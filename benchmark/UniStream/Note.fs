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

    type T =
        | Init
        | Active of Value

    let applyNoteCreated agg (ev: NoteCreated) =
        match agg with
        | Init -> { Title = ev.Title; Content = ev.Content; Count = 0 }
        | _ -> failwith "只有初始状态才能创建Note。"

    let applyNoteChanged agg (ev: NoteChanged) =
        match agg with
        | Active v -> { v with Content = ev.Content; Count = v.Count + 1 }
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

    let createNote ev agg (traceId: string) =
        let v = applyNoteCreated agg ev
        seq { noteCreated, Delta.asBytes ev, MetaData.correlationId v.Title }, Active v

    let changeNote ev agg (traceId: string) =
        let v = applyNoteChanged agg ev
        seq { noteChanged, Delta.asBytes ev, MetaData.correlationId v.Title }, Active v