namespace Note.Domain.ActorAgg


type Create = { Name: string }

module Actor =

    type T =
        | Init
        | Active of {| Name: string |}

    let inline actorCreated c t =
        match t with
        | Init ->
            let delta = (^c : (member Value: Create) c)
            Active  {| Name = delta.Name |}
        | Active _ -> failwith "只有初始状态才能创建Note。"