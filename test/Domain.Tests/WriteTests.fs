module Domain.Tests.Write

open System
open Expecto
open UniStream.Domain
open Domain


let stream =
    { new IStream with
        member _.Writer = writer
        member _.Reader = reader }

let opt = AggregateOptions(Capacity = 3)
let agent = Aggregator.init Note stream opt
Aggregator.register agent <| Replay<Note, NoteCreated>()
Aggregator.register agent <| Replay<Note, NoteChanged>()
Aggregator.register agent <| Replay<Note, NoteUpgraded>()
let traceId = None
let id1 = Guid.NewGuid()
let id2 = Guid.NewGuid()
let id3 = Guid.NewGuid()


[<Tests>]
let test =
    [ testCase "创建两个除Id外其他值相等的聚合"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          let agg = create agent traceId id1 com |> Async.RunSynchronously
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
          let agg = create agent traceId id2 com |> Async.RunSynchronously
          Expect.equal (agg.Revision, agg.Title, agg.Content, agg.Grade) (0UL, "t", "c", 1) "聚合值有误"
          Expect.notEqual id1 id2 "两个Id值有误"
      testCase "创建第三个聚合，存在不合验证逻辑的数据"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 4 }

          let f = fun _ -> create agent traceId id3 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "验证错误类型有误"
      testCase "第一个聚合应用第一条不会导致验证错误的变更"
      <| fun _ ->
          let com = { Up = 2 }
          let agg = upgrade agent traceId id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 1UL, "t", "c", 3) "聚合值有误"
      testCase "第二个聚合应用第一条会导致验证错误的变更"
      <| fun _ ->
          let com = { Up = 3 }
          let f = fun _ -> upgrade agent traceId id2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "第一个聚合应用第二条变更"
      <| fun _ ->
          let com = { Content = "c1" }
          let agg = change agent traceId id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 2UL, "t", "c1", 3) "聚合值有误"
      testCase "第二个聚合应用第二条变更"
      <| fun _ ->
          let com = { Content = "c1" }
          let agg = change agent traceId id2 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id2, 1UL, "t", "c1", 1) "聚合值有误"
      testCase "第一个聚合应用第三条变更"
      <| fun _ ->
          let com = { Content = "c2" }
          let agg = change agent traceId id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 3UL, "t", "c2", 3) "聚合值有误"
      testCase "第二个聚合应用第三条变更"
      <| fun _ ->
          let com = { Content = "c2" }
          let agg = change agent traceId id2 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id2, 2UL, "t", "c2", 1) "聚合值有误"
      testCaseAsync "并行应用领域变更"
      <| async {
          let coms =
              [ for i in 1..1000 ->
                    { Title = $"t{i}"
                      Content = $"c{i}"
                      Grade = 1 } ]

          let! r =
              coms
              |> List.map (fun c -> create agent traceId (Guid.NewGuid()) c)
              |> Async.Parallel

          Expect.allEqual (r |> Array.map (fun n -> n.Revision)) 0UL "聚合版本有误"
          Expect.hasLength r 1000 "返回集合长度有误"
      } ]
    |> testList "Write"
    |> testSequenced
    |> testLabel "Domain"
