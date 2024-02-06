module Domain.Tests.Init

open System
open System.Collections.Generic
open Expecto

open UniStream.Domain
open Domain


let agent = Aggregator.init Note writer reader 3 0.2
let traceId = Guid.NewGuid()
let id = Guid.NewGuid()


[<Tests>]
let test =
    [ testCase "创建聚合"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          let agg = create agent traceId id com |> Async.RunSynchronously
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
      testCase "持续应用六条变更，以便触发缩容"
      <| fun _ ->
          let c = [ for i in 1..6 -> { Content = $"c{i}" } ]
          let r = c |> List.map (fun c -> change agent traceId id c |> Async.RunSynchronously)
          let agg = r[5]
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (6UL, "t", "c6", 1) "聚合值有误"
      testCase "暂停以刷新缓存，然后应用第七条变更"
      <| fun _ ->
          Threading.Thread.Sleep 200
          let com = { Content = "c1" }
          let f = fun _ -> change agent traceId id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册有关重播，然后再次应用第七条变更"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, NoteCreated>()
          Aggregator.register agent <| Replay<Note, NoteChanged>()
          let com = { Content = "c1" }
          let agg = change agent traceId id com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id, 7UL, "t", "c1", 1) "聚合值有误"
      testCase "持续应用五条变更，以便触发缩容"
      <| fun _ ->
          let c = [ for i in 1..5 -> { Content = $"c{i}" } ]
          let r = c |> List.map (fun c -> change agent traceId id c |> Async.RunSynchronously)
          let agg = r[4]
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (12UL, "t", "c5", 1) "聚合值有误"
      testCase "应用第十三条未注册重播的变更"
      <| fun _ ->
          let com = { Up = 1 }
          let agg = upgrade agent traceId id com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id, 13UL, "t", "c5", 2) "聚合值有误"
      testCase "暂停以刷新缓存，然后应用第十四条变更"
      <| fun _ ->
          Threading.Thread.Sleep 200
          let com = { Content = "c2" }
          let f = fun _ -> change agent traceId id com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册其余重播，然后再次应用第十四条变更"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, NoteUpgraded>()
          let com = { Content = "c2" }
          let agg = change agent traceId id com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id, 14UL, "t", "c2", 2) "聚合值有误" ]
    |> testList "Init"
    |> testSequenced
    |> testLabel "Domain"
