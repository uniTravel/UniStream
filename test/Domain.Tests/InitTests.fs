module Domain.Tests.Init

open System
open System.Collections.Generic
open Expecto
open UniStream.Domain
open Domain


let stream =
    { new IStream with
        member _.Writer = writer
        member _.Reader = reader }

let opt = AggregateOptions(Capacity = 3)
let agent = Aggregator.init Note stream opt
let traceId = None
let i1 = Guid.NewGuid()
let i2 = Guid.NewGuid()


[<Tests>]
let test =
    [ testCase "创建第一个聚合"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          let agg = create agent traceId i1 com |> Async.RunSynchronously
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
      testCase "创建第二个聚合"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          let agg = create agent traceId i2 com |> Async.RunSynchronously
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
      testCase "第二个聚合持续应用八条变更，以便触发缩容"
      <| fun _ ->
          let c = [ for i in 1..8 -> { Content = $"c{i}" } ]
          let r = c |> List.map (fun c -> change agent traceId i2 c |> Async.RunSynchronously)
          let agg = r[7]
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (8UL, "t", "c8", 1) "聚合值有误"
      testCase "第一个聚合应用第一条变更"
      <| fun _ ->
          let com = { Content = "c1" }
          let f = fun _ -> change agent traceId i1 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册有关重播，然后第一个聚合再次应用第一条变更"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, NoteCreated>()
          Aggregator.register agent <| Replay<Note, NoteChanged>()
          let com = { Content = "c1" }
          let agg = change agent traceId i1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (i1, 1UL, "t", "c1", 1) "聚合值有误"
      testCase "第二个聚合应用第九条未注册重播的变更"
      <| fun _ ->
          let com = { Up = 1 }
          let agg = upgrade agent traceId i2 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (i2, 9UL, "t", "c8", 2) "聚合值有误"
      testCase "第一个聚合持续应用八条变更，以便触发缩容"
      <| fun _ ->
          let c = [ for i in 1..8 -> { Content = $"c{i}" } ]
          let r = c |> List.map (fun c -> change agent traceId i1 c |> Async.RunSynchronously)
          let agg = r[7]
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (9UL, "t", "c8", 1) "聚合值有误"
      testCase "第二个聚合应用第十条变更"
      <| fun _ ->
          let com = { Content = "c2" }
          let f = fun _ -> change agent traceId i2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册其余重播，然后第二个聚合再次应用第十条变更"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, NoteUpgraded>()
          let com = { Content = "c2" }
          let agg = change agent traceId i2 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (i2, 10UL, "t", "c2", 2) "聚合值有误" ]
    |> testList "Init"
    |> testSequenced
    |> testLabel "Domain"
