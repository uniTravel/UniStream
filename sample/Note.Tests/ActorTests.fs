module Actor.Tests

open System
open Expecto
open Note.Contract


[<Tests>]
let tests =
    let aggId = Guid.NewGuid()
    testList "ActorAgg" [
        testCase "Create Actor" <| fun _ ->
            let traceId = Guid.NewGuid()
            let command = CreateActor()
            command.Name <- "actor"
            let reply = app.CreateActor aggId traceId command |> Async.RunSynchronously
            Expect.equal reply.Name command.Name "返回值错误。"
    ]
    |> testLabel "Note App"