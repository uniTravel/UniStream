module Note.Tests

open System
open Expecto
open Note.Domain


[<Tests>]
let tests =
    let aggId1 = Guid.NewGuid()
    let aggId2 = Guid.NewGuid()
    let aggId3 = Guid.NewGuid()
    let title1 = Guid.NewGuid().ToString()
    let title2 = Guid.NewGuid().ToString()
    let title3 = Guid.NewGuid().ToString()
    testSequenced <| testList "可变聚合" [
        let withArgs f () =
            go "可变聚合" |> f
        yield! testFixture withArgs [
            "给定三个聚合ID，创建三个Note", fun finish ->
                let c1 : CreateNote = { Title = title1; Content = "initial content" }
                let c2 : CreateNote = { Title = title2; Content = "initial content" }
                let c3 : CreateNote = { Title = title3; Content = "initial content" }
                let r1 = app.CreateNote "test" aggId1 (Guid.NewGuid()) c1 |> Async.RunSynchronously
                let r2 = app.CreateNote "test" aggId2 (Guid.NewGuid()) c2 |> Async.RunSynchronously
                let r3 = app.CreateNote "test" aggId3 (Guid.NewGuid()) c3 |> Async.RunSynchronously
                Expect.equal r1.Title c1.Title "返回值错误。"
                Expect.equal r1.Content c1.Content "返回值错误。"
                Expect.equal r2.Title c2.Title "返回值错误。"
                Expect.equal r2.Content c2.Content "返回值错误。"
                Expect.equal r3.Title c3.Title "返回值错误。"
                Expect.equal r3.Content c3.Content "返回值错误。"
                finish 1
            "修改第一个Note", fun finish ->
                let c1 : ChangeNote = { Content = "changed content" }
                let r1 = app.ChangeNote "test" aggId1 (Guid.NewGuid()) c1 |> Async.RunSynchronously
                Expect.equal r1.Content c1.Content "返回值错误。"
                Threading.Thread.Sleep 10
                finish 2
            "获取三个Note观察者的值", fun finish ->
                let o1 = app.GetNoteObserver title1 |> Async.RunSynchronously
                let o2 = app.GetNoteObserver title2 |> Async.RunSynchronously
                let o3 = app.GetNoteObserver title3 |> Async.RunSynchronously
                Expect.equal o1.[0].Count 1 "返回值错误。"
                Expect.equal o2.[0].Count 0 "返回值错误。"
                Expect.equal o3.[0].Count 0 "返回值错误。"
                finish 3
            "获取一个不存在的Note观察者的值", fun finish ->
                let f = fun _ -> app.GetNoteObserver (Guid.NewGuid().ToString()) |> Async.RunSynchronously |> ignore
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 4
            "连续修改第二个Note", fun finish ->
                let c2 : ChangeNote = { Content = "changed content" }
                let r2 = app.ChangeNote "test" aggId2 (Guid.NewGuid()) c2 |> Async.RunSynchronously
                Expect.equal r2.Content c2.Content "返回值错误。"
                let c2 : ChangeNote = { Content = "changed content again" }
                let r2 = app.ChangeNote "test" aggId2 (Guid.NewGuid()) c2 |> Async.RunSynchronously
                Expect.equal r2.Content c2.Content "返回值错误。"
                Threading.Thread.Sleep 30
                finish 5
            "第二次获取三个Note观察者的值", fun finish ->
                let o1 = app.GetNoteObserver title1 |> Async.RunSynchronously
                let o2 = app.GetNoteObserver title2 |> Async.RunSynchronously
                let o3 = app.GetNoteObserver title3 |> Async.RunSynchronously
                Expect.equal o1.[0].Count 1 "返回值错误。"
                Expect.equal o2.[0].Count 2 "返回值错误。"
                Expect.equal o3.[0].Count 0 "返回值错误。"
                finish 6
            "批量修改第三个Note", fun finish ->
                seq { 1 .. 3 }
                |> Seq.map (fun i ->
                    let c3 : ChangeNote = { Content = "batch changed content" }
                    app.BatchChangeNote "test" aggId3 (Guid.NewGuid()) c3
                ) |> Async.Parallel |> Async.RunSynchronously |> ignore
                let r3 = app.GetNote <| aggId3.ToString() |> Async.RunSynchronously
                Expect.equal r3.Content "batch changed content" "返回值错误。"
                Threading.Thread.Sleep 100
                finish 7
            "第三次获取三个Note观察者的值", fun finish ->
                let o1 = app.GetNoteObserver title1 |> Async.RunSynchronously
                let o2 = app.GetNoteObserver title2 |> Async.RunSynchronously
                let o3 = app.GetNoteObserver title3 |> Async.RunSynchronously
                Expect.equal o1.[0].Count 1 "返回值错误。"
                Expect.equal o2.[0].Count 2 "返回值错误。"
                Expect.equal o3.[0].Count 3 "返回值错误。"
                finish 8
        ]
    ]
    |> testLabel "Note App"