module Domain.Tests.Write

open System
open Expecto

open UniStream.Domain
open Domain


let agent = Aggregator.init Note writer reader 10000 1.0
Aggregator.register agent <| Replay<Note, CreateNote>()
Aggregator.register agent <| Replay<Note, ChangeNote>()
Aggregator.register agent <| Replay<Note, UpgradeNote>()

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
      testCase "创建第三个聚合，存在不合验证逻辑的数据"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 4 }

          let f = fun _ -> create agent com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "验证错误类型有误"
      testCase "第一个聚合应用第一条命令，不会导致验证错误"
      <| fun _ ->
          let com = { Up = 2 }
          let agg = upgrade agent id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 1UL, "t", "c", 3) "聚合值有误"
      testCase "第二个聚合应用第一条命令，会导致验证错误"
      <| fun _ ->
          let com = { Up = 3 }
          let f = fun _ -> upgrade agent id2 com |> Async.RunSynchronously |> ignore
          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "第一个聚合应用第二条命令"
      <| fun _ ->
          let com = { Content = "c1" }
          let agg = change agent id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 2UL, "t", "c1", 3) "聚合值有误"
      testCase "第二个聚合应用第二条命令"
      <| fun _ ->
          let com = { Content = "c1" }
          let agg = change agent id2 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id2, 1UL, "t", "c1", 1) "聚合值有误"
      testCase "暂停1秒，然后第一个聚合应用第三条命令"
      <| fun _ ->
          Threading.Thread.Sleep 1000
          let com = { Content = "c2" }
          let agg = change agent id1 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id1, 3UL, "t", "c2", 3) "聚合值有误"
      testCase "第二个聚合应用第三条命令"
      <| fun _ ->
          let com = { Content = "c2" }
          let agg = change agent id2 com |> Async.RunSynchronously
          Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content, agg.Grade) (id2, 2UL, "t", "c2", 1) "聚合值有误"
      testCaseAsync "并行应用领域命令"
      <| async {
          let coms =
              [ for i in 1..1000 ->
                    { Title = $"t{i}"
                      Content = $"c{i}"
                      Grade = 1 } ]

          let! r = coms |> List.map (fun c -> create agent c) |> Async.Parallel
          Expect.allEqual (r |> Array.map (fun n -> n.Revision)) 0UL "聚合版本有误"
          Expect.hasLength r 1000 "返回集合长度有误"
      } ]
    |> testList "Write"
    |> testSequenced
    |> testLabel "Domain"
