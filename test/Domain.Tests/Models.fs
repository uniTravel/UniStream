namespace Domain.Models

open UniStream.Domain


type CreateNote = { Title: string; Content: string }

type ChangeNote = { Content: string }

type NoteCreated = { Title: string; Content: string }

type NoteChanged = { Content: string }

[<Sealed>]
type Note(id) as me =
    inherit Aggregate(id)

    let created ev =
        me.Title <- ev.Title
        me.Content <- ev.Content

    let changed ev = me.Content <- ev.Content

    let corrected (agg: Note) =
        me.Title <- agg.Title
        me.Content <- agg.Content

    let create (cm: CreateNote) =
        let ev =
            { Title = cm.Title
              Content = cm.Content }

        created ev
        nameof NoteCreated, Delta.serialize ev

    let change (cm: ChangeNote) =
        let ev = { Content = cm.Content }
        changed { Content = cm.Content }
        nameof NoteChanged, Delta.serialize ev

    member val Title = "" with get, set
    member val Content = "" with get, set

    override _.Apply cmType cm =
        match cmType with
        | nameof CreateNote -> create <| Delta.deserialize cm
        | nameof ChangeNote -> change <| Delta.deserialize cm
        | _ -> failwith $"领域命令类型{cmType}不存在。"

    override _.Replay evType ev =
        match evType with
        | nameof NoteCreated -> created <| Delta.deserialize ev
        | nameof NoteChanged -> changed <| Delta.deserialize ev
        | nameof Note -> corrected <| Delta.deserialize ev
        | _ -> failwith $"领域事件类型{evType}不存在。"
