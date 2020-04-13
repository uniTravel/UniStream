namespace Note.Domain

open UniStream.Domain


type Created = { Title: string; Content: string }

type Changed = { Content: string }


module NoteObserver =

    type Value = { Title: string; Content: string; Count: int }

    type T =
        | Init
        | Active of Value

    let applyNoteCreated agg (ev: Created) =
        match agg with
        | Init -> { Title = ev.Title; Content = ev.Content; Count = 0 }
        | _ -> failwith "只有初始状态才能创建Note。"

    let applyNoteChanged agg (ev: Changed) =
        match agg with
        | Active v -> { v with Content = ev.Content; Count = v.Count + 1 }
        | _ -> failwith "只有Active状态才能改变Note。"

    let apply agg evType evBytes =
        match evType with
        | ev when ev = "Note.Domain.NoteCreated" ->
            let ev = Delta.fromBytes<Created> evBytes
            applyNoteCreated agg ev |> Active
        | ev when ev = "Note.Domain.NoteChanged" ->
            let ev = Delta.fromBytes<Changed> evBytes
            applyNoteChanged agg ev |> Active
        | _ -> failwithf "领域事件类型错误：%s" evType

    type T with
        static member Initial = Init
        member this.ApplyEvent = apply this
        member this.Value =
            match this with
            | Active v -> v
            | Init -> failwith "初始状态，尚未赋值。"