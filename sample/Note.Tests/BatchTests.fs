module Note.Tests.Batch

open System
open Expecto
open Note.Contract


[<Tests>]
let tests =
    let aggId = Guid.NewGuid().ToString()
    testSequenced <| testList "可变聚合批处理" [
        testCase "给定聚合ID，创建Note" <| fun _ ->
            let c = { Title = "title"; Content = "initial content" }
            let r = app.BatchCreate "test" aggId (Guid.NewGuid().ToString()) c |> Async.RunSynchronously
            Expect.equal r.Content c.Content "返回值错误。"
        testCase "连续修改Note" <| fun _ ->
            let r =
                seq { 1 .. 5000 }
                |> Seq.map (fun i -> app.BatchChange "test" aggId (Guid.NewGuid().ToString()) { Content = "changed content" })
                |> Async.Parallel
                |> Async.RunSynchronously
            Expect.equal r.Length 5000 "返回值数量错误。"
            Threading.Thread.Sleep 50
    ]
    |> testLabel "Note App"