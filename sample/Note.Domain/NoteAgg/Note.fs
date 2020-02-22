namespace Note.Domain

open UniStream.Domain
open Note.Contract


[<CLIMutable>]
type NoteCreated = { Title: string; Content: string }

[<CLIMutable>]
type NoteChanged = { Content: string }


module Note =

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

    let apply (t: T) (evType: string) (evBytes: byte[]) : T =
        match evType with
        | "Note.Domain.NoteCreated" ->
            let ev = Delta.fromBytes<NoteCreated> evBytes
            applyNoteCreated t ev |> Active
        | "Note.Domain.NoteChanged" ->
            let ev = Delta.fromBytes<NoteChanged> evBytes
            applyNoteChanged t ev |> Active
        | _ -> failwithf "领域事件类型错误：%s" evType

    type T with
        static member Initial = Init
        member this.ApplyEvent = apply this

    let createNote (cv: CreateNote) t =
        let ev : NoteCreated = { Title = cv.Title; Content = cv.Content }
        let value = applyNoteCreated t ev
        [| typeof<NoteCreated>.FullName, Delta.asBytes ev |], Active value

    let changeNote (cv: ChangeNote) (t: T) : (string * byte[])[] * T =
        let ev : NoteChanged = { Content = cv.Content }
        let value = applyNoteChanged t ev
        [| typeof<NoteChanged>.FullName, Delta.asBytes ev |], Active value