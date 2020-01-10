namespace Note.Domain

open UniStream.Domain
open Note.Contract

module Actor =

    type T =
        | Init
        | Active of {| Name: string |}

    let actorCreated (delta: CreateActor) t =
        match t with
        | Init -> Active  {| Name = delta.Name |}
        | Active _ -> failwith "只有初始状态才能创建Note。"

    let apply t deltaType deltaBytes : T =
        match deltaType with
        | "Note.Contract.CreateActor" ->
            let delta = Delta.fromBytes<CreateActor> deltaBytes
            actorCreated delta t
        | d -> failwithf "边际影响类型错误：%s" d

    type T with
        static member Empty = Init
        member this.Apply  = apply this