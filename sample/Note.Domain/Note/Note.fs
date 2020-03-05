namespace Note.Domain

open UniStream.Domain


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

    let apply t evType evBytes =
        match evType with
        | ev when ev = noteCreated ->
            let ev = Delta.fromBytes<NoteCreated> evBytes
            applyNoteCreated t ev |> Active
        | ev when ev = noteChanged ->
            let ev = Delta.fromBytes<NoteChanged> evBytes
            applyNoteChanged t ev |> Active
        | _ -> failwithf "领域事件类型错误：%s" evType

    type T with
        static member Initial = Init
        static member AggMode = Mutable
        member this.ApplyEvent = apply this
        member this.Value =
            match this with
            | Active v -> v
            | Init -> failwith "初始状态，尚未赋值。"

    let createNote ev t =
        [| noteCreated, Delta.asBytes ev |], Active <| applyNoteCreated t ev

    let changeNote ev t =
        [| noteChanged, Delta.asBytes ev |], Active <| applyNoteChanged t ev