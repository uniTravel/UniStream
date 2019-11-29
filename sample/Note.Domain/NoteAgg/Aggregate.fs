namespace Note.Domain.NoteAgg

open UniStream.Abstract


module Note =

    type Active = { Title: string; Content: string; Version: int }

    type T =
        | Init
        | Active of Active
        with interface IAggregate

    let noteCreated wrap t =
        match t with
        | Init ->
            let v = (wrap :> IWrapped<CreateNote>).Value
            let agg = { Title = v.Title; Content = v.Content; Version = 0 }
            Active agg, agg.Version, v
        | Active _ -> failwith "只有初始状态才能创建Note。"

    let noteChanged wrap t =
        match t with
        | Init -> failwith "初始状态不能更改Note。"
        | Active agg ->
            let v = (wrap :> IWrapped<ChangeNote>).Value
            let agg' = { agg with Content = v.Content; Version = agg.Version + 1 }
            Active agg', agg.Version, v