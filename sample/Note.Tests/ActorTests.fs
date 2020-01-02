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
            let command = CreateActor.create { Name = "actor" }
            Async.RunSynchronously <| applyCommand actor aggId traceId command
    ]
    |> testLabel "Note App"