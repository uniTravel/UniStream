namespace Note.Domain.ActorAgg


type ActorCreated = { Name: string }

module Actor =

    type T =
        | Init
        | Active of {| Name: string |}

    let actorCreated delta t =
        match t with
        | Init -> Active  {| Name = delta.Name |}
        | Active _ -> failwith "只有初始状态才能创建Note。"

    type T with
        static member Empty = Init
        member this.Apply : (string -> byte[] -> T) = failwith ""