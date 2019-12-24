module Actor.Tests

open System
open Expecto
open Note.Domain.ActorAgg


[<Tests>]
let tests =
    testList "ActorAgg" [
        testCase "Create Actor" <| fun _ ->
            let aggId = Guid.NewGuid()
            let traceId = Guid.NewGuid()
            let name = typeof<ActorCreated>.FullName
            let cb = [||]
            Async.Start <| applyRaw actor aggId traceId name cb CreateActor.create
    ]
    |> testLabel "Note App"