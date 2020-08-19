namespace Note.Domain

open System
open UniStream.Domain


type CreateActorCommand = { Name: string }

type ActorValue = { Name: string; Sex: string }

type ActorCreated = { Name: string }


module Actor =

    let actorCreated = typeof<ActorCreated>.FullName

    type T =
        | Init
        | Active of ActorValue
        | Close of ActorValue

    let applyActorCreated (ev: ActorCreated) =
        { Name = ev.Name; Sex = "Male" }

    let apply agg evType data =
        match agg, evType with
        | Init, ev when ev = actorCreated ->
            let ev = Delta.deserialize<ActorCreated> data
            Active <| applyActorCreated ev
        | _ -> failwithf "领域事件值类型为%s。" evType

    let createActor (cv: CreateActorCommand) (agg: T) : ((string * ReadOnlyMemory<byte>) seq * T) =
        match agg with
        | Init ->
            let ev = { Name = cv.Name }
            seq { actorCreated, Delta.serialize ev }, Active <| applyActorCreated ev
        | _ -> failwith "只有初始状态才能创建Actor。"

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