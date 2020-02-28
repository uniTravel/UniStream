namespace Note.Domain

open UniStream.Domain


type NoteCreated = { Title: string; Content: string }

type NoteChanged = { Content: string }


module Note =

    let noteCreated = typeof<NoteCreated>.FullName
    let noteChanged = typeof<NoteChanged>.FullName

    type Value =
        { Title: string; Content: string }

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
        member this.ApplyEvent = apply this

    let createNote ev t =
        [| noteCreated, Delta.asBytes ev |], Active <| applyNoteCreated t ev

    let changeNote ev t =
        [| noteChanged, Delta.asBytes ev |], Active <| applyNoteChanged t ev