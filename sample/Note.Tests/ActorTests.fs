module Actor.Tests

open System
open Expecto
open Note.Domain


[<Tests>]
let tests =
    let aggId = Guid.NewGuid()
    testSequenced <| testList "不可变聚合" [
        let withArgs f () =
            go "不可变聚合" |> f
        yield! testFixture withArgs [
            "给定聚合ID，创建Actor", fun finish ->
                let traceId = Guid.NewGuid()
                let command : CreateActor = { Name = "actor" }
                let reply = app.CreateActor "test" aggId traceId command |> Async.RunSynchronously
                Expect.equal reply.Name command.Name "返回值错误。"
                finish 1
            "同一聚合ID，重复创建Actor", fun finish ->
                let traceId = Guid.NewGuid()
                let command : CreateActor = { Name = "actor" }
                let f = fun _ -> app.CreateActor "test" aggId traceId command |> Async.RunSynchronously |> ignore
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 2
        ]
    ]
    |> testLabel "Note App"