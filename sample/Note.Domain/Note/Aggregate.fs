namespace Note.Domain

open UniStream.Domain


type NoteCreated = { Title: string; Content: string }

type NoteChanged = { Content: string }

module Note =

    type T =
        | Init
        | Active of {| Title: string; Content: string |}

    let noteCreated delta t =
        match t with
        | Init -> Active  {| Title = delta.Title; Content = delta.Content |}
        | Active _ -> failwith "只有初始状态才能创建Note。"

    let noteChanged delta t =
        match t with
        | Init -> failwith "初始状态不能更改Note。"
        | Active agg ->
            let agg' = {| agg with Content = delta.Content |}
            Active agg'

    let apply t deltaType deltaBytes : T =
        match deltaType with
        | "Note.Domain.NoteAgg.NoteCreated" ->
            let delta = Delta.fromBytes<NoteCreated> deltaBytes
            noteCreated delta t
        | "Note.Domain.NoteAgg.NoteChanged" ->
            let delta = Delta.fromBytes<NoteChanged> deltaBytes
            noteChanged delta t
        | d -> failwithf "边际影响类型错误：%s" d

    type T with
        static member Empty = Init
        member this.Apply = apply this