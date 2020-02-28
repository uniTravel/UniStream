namespace Note.Domain

open UniStream.Domain


type ActorCreated = { Name: string }


module Actor =

    let actorCreated = typeof<ActorCreated>.FullName

    type Value =
        { Name: string }

    type T =
        | Init
        | Active of Value

    let applyActorCreated t (ev: ActorCreated) =
        match t with
        | Init -> { Name = ev.Name }
        | Active _ -> failwith "只有初始状态才能创建Actor。"

    let apply t evType evBytes =
        match evType with
        | ev when ev = actorCreated ->
            let ev = Delta.fromBytes<ActorCreated> evBytes
            Active <| applyActorCreated t ev
        | _ -> failwithf "领域事件值类型错误：%s" evType

    type T with
        static member Initial = Init
        member this.ApplyEvent  = apply this

    let createActor ev t =
        [| actorCreated, Delta.asBytes ev |], Active <| applyActorCreated t ev