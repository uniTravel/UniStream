module Actor.Tests

open System
open Expecto
open ApiConfig
open Note.Domain.ActorAgg


[<Tests>]
let tests =
    testList "ActorAgg" [
        testCase "Create Actor" <| fun _ ->
            let aggId = Guid.NewGuid()
            let cb = [||]
            Async.Start <| applyRaw aggId cb CreateActor.create
    ]
    |> testLabel "Note App"