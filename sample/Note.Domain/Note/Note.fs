namespace Note.Domain

open UniStream.Domain


type CreateNoteCommand = { Title: string; Content: string }

type ChangeNoteCommand = { Content: string }

type NoteValue = { Title: string; Content: string; Count: int }

type NoteCreated = { Title: string; Content: string }

type NoteChanged = { Content: string }


module Note =

    let noteCreated = typeof<NoteCreated>.FullName
    let noteChanged = typeof<NoteChanged>.FullName

    type T =
        | Init
        | Active of NoteValue
        | Close of NoteValue

    let applyNoteCreated (ev: NoteCreated) =
        { Title = ev.Title; Content = ev.Content; Count = 0 }

    let applyNoteChanged note (ev: NoteChanged) =
        { note with Content = ev.Content; Count = note.Count + 1 }

    let applyEvent agg evType data =
        match agg, evType with
        | Init, ev when ev = noteCreated ->
            Delta.deserialize<NoteCreated> data |> applyNoteCreated |> Active
        | Active note, ev when ev = noteChanged ->
            Delta.deserialize<NoteChanged> data |> applyNoteChanged note |> Active
        | _ -> failwithf "领域事件类型为%s。" evType

    let createNote (cv: CreateNoteCommand) agg =
        match agg with
        | Init ->
            let ev = { Title = cv.Title; Content = cv.Content }
            seq { noteCreated, Delta.serialize ev }, Active <| applyNoteCreated ev
        | _ -> failwith "只有初始状态才能创建Note。"

    let changeNote (cv: ChangeNoteCommand) agg =
        match agg with
        | Active note ->
            let ev = { Content = cv.Content }
            seq { noteChanged, Delta.serialize ev }, Active <| applyNoteChanged note ev
        | _ -> failwith "只有Active状态才能改变Note。"

    type T with
        static member Initial = Init
        member this.ApplyEvent = applyEvent this
        member this.Value =
            match this with
            | Active v | Close v -> v
            | Init -> failwith "初始状态，尚未赋值。"
        member this.Closed =
            match this with
            | Close _ -> true
            | _ -> false