namespace Note.Domain

open UniStream.Domain
open Note.Contract


type ActorCreated = { Name: string }


module Actor =

    let actorCreated = typeof<ActorCreated>.FullName

    type T =
        | Init
        | Active of Actor
        | Close of Actor

    let applyActorCreated agg (ev: ActorCreated) =
        match agg with
        | Init -> { Name = ev.Name; Sex = "Male" }
        | _ -> failwith "只有初始状态才能创建Actor。"

    let apply agg evType data =
        match evType with
        | ev when ev = actorCreated ->
            let ev = Delta.deserialize<ActorCreated> data
            Active <| applyActorCreated agg ev
        | _ -> failwithf "领域事件值类型错误：%s" evType

    type T with
        static member Initial = Init
        member this.ApplyEvent = apply this
        member this.Value =
            match this with
            | Active v | Close v -> v
            | Init -> failwith "初始状态，尚未赋值。"
        member this.Closed =
            match this with
            | Close _ -> true
            | _ -> false


    let createActor ev agg =
        seq { actorCreated, Delta.serialize ev }, Active <| applyActorCreated agg ev