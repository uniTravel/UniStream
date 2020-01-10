namespace Note.Domain

open UniStream.Domain
open Note.Contract


module Note =

    type T =
        | Init
        | Active of {| Title: string; Content: string |}

    let noteCreated (delta: CreateNote) t =
        match t with
        | Init -> Active  {| Title = delta.Title; Content = delta.Content |}
        | Active _ -> failwith "只有初始状态才能创建Note。"

    let noteChanged (delta: ChangeNote) t =
        match t with
        | Init -> failwith "初始状态不能更改Note。"
        | Active agg ->
            let agg' = {| agg with Content = delta.Content |}
            Active agg'

    let apply t deltaType deltaBytes : T =
        match deltaType with
        | "Note.Contract.CreateNote" ->
            let delta = Delta.fromBytes<CreateNote> deltaBytes
            noteCreated delta t
        | "Note.Contract.ChangeNote" ->
            let delta = Delta.fromBytes<ChangeNote> deltaBytes
            noteChanged delta t
        | d -> failwithf "边际影响类型错误：%s" d

    type T with
        static member Empty = Init
        member this.Apply = apply this