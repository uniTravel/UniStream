module Note.Tests.Basic

open System
open Expecto
open Note.Contract


[<Tests>]
let tests =
    let aggId1 = Guid.NewGuid().ToString()
    let aggId2 = Guid.NewGuid().ToString()
    let aggId3 = Guid.NewGuid().ToString()
    let title1 = Guid.NewGuid().ToString()
    let title2 = Guid.NewGuid().ToString()
    let title3 = Guid.NewGuid().ToString()
    let createNote = typeof<CreateNote>.FullName
    let changeNote = typeof<ChangeNote>.FullName
    testSequenced <| testList "可变聚合基本处理" [
        testCase "给定三个聚合ID，创建三个Note" <| fun _ ->
            let c1 = { Title = title1; Content = "initial content" }
            let c2 = { Title = title2; Content = "initial content" }
            let c3 = { Title = title3; Content = "initial content" }
            let r1 = app.CreateNote "test" aggId1 createNote (Guid.NewGuid().ToString()) c1 |> Async.RunSynchronously
            let r2 = app.CreateNote "test" aggId2 createNote (Guid.NewGuid().ToString()) c2 |> Async.RunSynchronously
            let r3 = app.CreateNote "test" aggId3 createNote (Guid.NewGuid().ToString()) c3 |> Async.RunSynchronously
            Expect.equal r1.Title c1.Title "返回值错误。"
            Expect.equal r1.Content c1.Content "返回值错误。"
            Expect.equal r2.Title c2.Title "返回值错误。"
            Expect.equal r2.Content c2.Content "返回值错误。"
            Expect.equal r3.Title c3.Title "返回值错误。"
            Expect.equal r3.Content c3.Content "返回值错误。"
        testCase "修改第一个Note" <| fun _ ->
            let c1 = { Content = "changed content" }
            let r1 = app.ChangeNote "test" aggId1 changeNote (Guid.NewGuid().ToString()) c1 |> Async.RunSynchronously
            Expect.equal r1.Content c1.Content "返回值错误。"
        testCase "连续修改第二个Note" <| fun _ ->
            let c2 = { Content = "changed content" }
            let r2 = app.ChangeNote "test" aggId2 changeNote (Guid.NewGuid().ToString()) c2 |> Async.RunSynchronously
            Expect.equal r2.Content c2.Content "返回值错误。"
            let c2 = { Content = "changed content again" }
            let r2 = app.ChangeNote "test" aggId2 changeNote (Guid.NewGuid().ToString()) c2 |> Async.RunSynchronously
            Expect.equal r2.Content c2.Content "返回值错误。"
            Threading.Thread.Sleep 50
    ]
    |> testLabel "Note App"