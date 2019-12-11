namespace Note.Domain.NoteAgg


module Note =

    type Active = { Title: string; Content: string }

    type T =
        | Init
        | Active of Active

    let inline noteCreated c t =
        match t with
        | Init ->
            let delta = (^c : (member Value: Create) c)
            let agg' = { Title = delta.Title; Content = delta.Content }
            Active agg'
        | Active _ -> failwith "只有初始状态才能创建Note。"

    let inline noteChanged c t =
        match t with
        | Init -> failwith "初始状态不能更改Note。"
        | Active agg ->
            let delta = (^c : (member Value: Change) c)
            let agg' = { agg with Content = delta.Content }
            Active agg'