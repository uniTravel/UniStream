namespace Note.Domain

open System
open UniStream.Domain
open Note.Contract


type ActorCreated = { Name: string }


module Actor =

    let actorCreated = typeof<ActorCreated>.FullName

    let createActor = typeof<CreateActor>.FullName

    type T =
        | Init
        | Active of Actor
        | Close of Actor

    let applyActorCreated agg (ev: ActorCreated) =
        match agg with
        | Init -> { Name = ev.Name; Sex = "Male" }
        | _ -> failwith "只有初始状态才能创建Actor。"

    let applyEvent agg evType data =
        match evType with
        | ev when ev = actorCreated ->
            let ev = Delta.deserialize<ActorCreated> data
            Active <| applyActorCreated agg ev
        | _ -> failwithf "领域事件值类型错误：%s" evType

    let applyCommand agg cvType data =
        match cvType with
        | cv when cv = createActor ->
            let cv = Delta.deserialize<CreateActor> data
            let ev = { Name = cv.Name }
            seq { actorCreated, Delta.serialize ev }, Active <| applyActorCreated agg ev
        | _ -> failwithf "领域命令值类型错误：%s" cvType

    type T with
        static member Initial = Init
        member this.ApplyEvent = applyEvent this
        member this.ApplyCommand = applyCommand this
        member this.Value =
            match this with
            | Active v | Close v -> v
            | Init -> failwith "初始状态，尚未赋值。"
        member this.Closed =
            match this with
            | Close _ -> true
            | _ -> false