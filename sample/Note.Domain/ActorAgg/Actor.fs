namespace Note.Domain

open UniStream.Domain
open Note.Contract


[<CLIMutable>]
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
            applyActorCreated t ev |> Active
        | _ -> failwithf "领域事件值类型错误：%s" evType

    type T with
        static member Initial = Init
        member this.ApplyEvent  = apply this

    let createActor (cv: CreateActor) t =
        let ev : ActorCreated = { Name = cv.Name }
        let value = applyActorCreated t ev
        [| actorCreated, Delta.asBytes ev |], Active value