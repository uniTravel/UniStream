module Domain.Tests.Cache

open System
open System.Collections.Generic
open Expecto

open UniStream.Domain
open Domain


let agent = Aggregator.init Note writer reader 10000 0.2
let mutable id1 = Guid.Empty
let mutable id2 = Guid.Empty


[<Tests>]
let test =
    [ testCase "创建两个除Id外其他值相等的聚合"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          let agg = create agent com |> Async.RunSynchronously
          id1 <- agg.Id
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
          let agg = create agent com |> Async.RunSynchronously
          id2 <- agg.Id
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
          Expect.notEqual id1 id2 "两个Id值有误"
      testCase "暂停以触发第一次缓存刷新，然后第一个聚合应用第一条命令"
      <| fun _ ->
          Threading.Thread.Sleep 200
          let com = { Content = "c1" }
          let agg = change agent id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 1UL, "t", "c1", 1) "聚合值有误"
      testCase "再暂停以触发第二次缓存刷新，然后第一个聚合应用第二条命令"
      <| fun _ ->
          Threading.Thread.Sleep 200
          let com = { Content = "c2" }
          let agg = change agent id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 2UL, "t", "c2", 1) "聚合值有误"
      testCase "第二个聚合应用第一条命令"
      <| fun _ ->
          let com = { Content = "c1" }
          let f = fun _ -> change agent id2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误" ]
    |> testList "Cache"
    |> testSequenced
    |> testLabel "Domain"
