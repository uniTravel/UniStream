module Note.Tests.Parallel

open System
open Expecto
open Note.Domain


let task count =
    seq { 0 .. count - 1 }
    |> Seq.map (fun i -> async {
        let aggId = Guid.NewGuid().ToString()
        let traceId = Guid.NewGuid().ToString()
        let command : CreateNoteCommand = { Title = "title"; Content = "initial content" }
        let! note = app.CreateNote "benchmark" aggId traceId command
        let traceId = Guid.NewGuid().ToString()
        let! note = app.ChangeNote "benchmark" aggId traceId { Content = "changed content" }
        () })

[<FTests>]
let tests =
    testList "可变聚合并行处理" [
        testCase "给定并行数量，并行创建并修改Note" <| fun _ ->
            let r = task 500 |> Async.Parallel |> Async.RunSynchronously
            Expect.equal r.Length 500 "返回集合大小错误。"
    ]
    |> testLabel "Note App"