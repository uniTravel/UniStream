namespace Note.Domain

open UniStream.Domain
open Note.Contract


[<CLIMutable>]
type NoteCreated = { Title: string; Content: string }

[<CLIMutable>]
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

    let createNote (cv: CreateNote) t =
        let ev : NoteCreated = { Title = cv.Title; Content = cv.Content }
        let value = applyNoteCreated t ev
        [| noteCreated, Delta.asBytes ev |], Active value

    let changeNote (cv: ChangeNote) t =
        let ev : NoteChanged = { Content = cv.Content }
        let value = applyNoteChanged t ev
        [| noteChanged, Delta.asBytes ev |], Active value