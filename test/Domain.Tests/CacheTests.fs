module Domain.Tests.Cache

open System
open System.Collections.Generic
open Expecto

open UniStream.Domain
open Domain


let agent = Aggregator.init Note writer reader 10000 0.2
Aggregator.register agent <| Replay<Note, NoteCreated>()
let traceId = Guid.NewGuid()
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

          let agg = create agent traceId com |> Async.RunSynchronously
          id1 <- agg.Id
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
          let agg = create agent traceId com |> Async.RunSynchronously
          id2 <- agg.Id
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
      testCase "两个聚合分别应用第一条变更，以写入缓存"
      <| fun _ ->
          let com = { Content = "c0" }
          let agg = change agent traceId id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 1UL, "t", "c0", 1) "聚合值有误"
          let agg = change agent traceId id2 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id2, 1UL, "t", "c0", 1) "聚合值有误"
      testCase "暂停以触发第一次缓存刷新，然后第一个聚合应用第二条变更"
      <| fun _ ->
          Threading.Thread.Sleep 200
          let com = { Content = "c1" }
          let agg = change agent traceId id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 2UL, "t", "c1", 1) "聚合值有误"
      testCase "再暂停以触发第二次缓存刷新，然后第一个聚合应用第三条变更"
      <| fun _ ->
          Threading.Thread.Sleep 200
          let com = { Content = "c2" }
          let agg = change agent traceId id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 3UL, "t", "c2", 1) "聚合值有误"
      testCase "第二个聚合应用第一条变更"
      <| fun _ ->
          let com = { Content = "c1" }
          let f = fun _ -> change agent traceId id2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误" ]
    |> testList "Cache"
    |> testSequenced
    |> testLabel "Domain"
