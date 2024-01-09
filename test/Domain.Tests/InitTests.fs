module Domain.Tests.Init

open System
open System.Collections.Generic
open Expecto

open UniStream.Domain
open Domain


let agent = Aggregator.init Note writer reader 10000 0.2
let mutable id = Guid.Empty


[<Tests>]
let test =
    [ testCase "创建聚合"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          let agg = create agent com |> Async.RunSynchronously
          id <- agg.Id
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
      testCase "暂停以刷新两次缓存，然后应用第一条命令"
      <| fun _ ->
          Threading.Thread.Sleep 400
          let com = { Content = "c1" }
          let f = fun _ -> change agent id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册有关重播，然后再次应用第一条命令"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, CreateNote>()
          Aggregator.register agent <| Replay<Note, ChangeNote>()
          let com = { Content = "c1" }
          let agg = change agent id com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id, 1UL, "t", "c1", 1) "聚合值有误"
      testCase "应用第二条未注册重播的命令"
      <| fun _ ->
          let com = { Up = 1 }
          let agg = upgrade agent id com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id, 2UL, "t", "c1", 2) "聚合值有误"
      testCase "暂停以第三次刷新缓存，然后应用第三条命令"
      <| fun _ ->
          Threading.Thread.Sleep 400
          let com = { Content = "c1" }
          let f = fun _ -> change agent id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册其余重播，然后再次应用第三条命令"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, UpgradeNote>()
          let com = { Content = "c2" }
          let agg = change agent id com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id, 3UL, "t", "c2", 2) "聚合值有误" ]
    |> testList "Init"
    |> testSequenced
    |> testLabel "Domain"
