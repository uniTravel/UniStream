namespace Note.Domain

open UniStream.Domain


type ActorCreated = { Name: string }

module Actor =

    type Value =
        { Name: string }

    let actorCreated = typeof<ActorCreated>.FullName

    type T =
        | Init
        | Active of Value

    let applyActorCreated agg (ev: ActorCreated) =
        match agg with
        | Init -> { Name = ev.Name }
        | Active _ -> failwith "只有初始状态才能创建Actor。"

    let apply agg evType evBytes =
        match evType with
        | ev when ev = actorCreated ->
            let ev = Delta.fromBytes<ActorCreated> evBytes
            Active <| applyActorCreated agg ev
        | _ -> failwithf "领域事件值类型错误：%s" evType

    type T with
        static member Initial = Init
        member this.ApplyEvent = apply this
        member this.Value =
            match this with
            | Active v -> v
            | Init -> failwith "初始状态，尚未赋值。"

    let createActor ev agg (metadata: byte[]) =
        try Ok ( seq { actorCreated, Delta.asBytes ev, metadata }, Active <| applyActorCreated agg ev)
        with ex -> Error ex.Message