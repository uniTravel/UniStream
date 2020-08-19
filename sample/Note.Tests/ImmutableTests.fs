module Note.Tests.Immutable

open System
open Expecto
open Note.Domain


[<Tests>]
let tests =
    let aggId = Guid.NewGuid().ToString()
    testSequenced <| testList "不可变聚合" [
        testCase "给定聚合ID，创建Actor" <| fun _ ->
            let traceId = Guid.NewGuid().ToString()
            let command = { Name = "actor" }
            let reply = app.CreateActor "test" aggId traceId command |> Async.RunSynchronously
            Expect.equal reply.Name command.Name "返回值错误。"
        testCase "同一聚合ID，重复创建Actor" <| fun _ ->
            let traceId = Guid.NewGuid().ToString()
            let command = { Name = "actor" }
            let f = fun _ -> app.CreateActor "test" aggId traceId command |> Async.RunSynchronously |> ignore
            Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
            Threading.Thread.Sleep 50
    ]
    |> testLabel "Note App"