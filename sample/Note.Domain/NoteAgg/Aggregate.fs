namespace Note.Domain.NoteAgg


type Create = { Title: string; Content: string }

type Change = { Content: string }

module Note =

    type T =
        | Init
        | Active of {| Title: string; Content: string |}

    let inline noteCreated c t =
        match t with
        | Init ->
            let delta = (^c : (member Value: Create) c)
            Active  {| Title = delta.Title; Content = delta.Content |}
        | Active _ -> failwith "只有初始状态才能创建Note。"

    let inline noteChanged c t =
        match t with
        | Init -> failwith "初始状态不能更改Note。"
        | Active agg ->
            let delta = (^c : (member Value: Change) c)
            let agg' = {| agg with Content = delta.Content |}
            Active agg'