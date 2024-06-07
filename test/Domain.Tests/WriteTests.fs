module Domain.Tests.Write

open System
open Expecto
open UniStream.Domain
open Domain


let stream =
    { new IStream with
        member _.Writer = writer "Note"
        member _.Reader = reader "Note"
        member _.Restore = restore "Note" }

let opt = AggregateOptions(Capacity = 3)
let agent = Aggregator.init Note stream opt
Aggregator.register agent <| Replay<Note, NoteCreated>()
Aggregator.register agent <| Replay<Note, NoteChanged>()
Aggregator.register agent <| Replay<Note, NoteUpgraded>()
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

          create agent id1 (Guid.NewGuid()) com |> Async.RunSynchronously
          create agent id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "创建第三个聚合，存在不合验证逻辑的数据"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 4 }

          let f =
              fun _ -> create agent id3 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "验证错误类型有误"
      testCase "第一个聚合应用第一条不会导致验证错误的变更"
      <| fun _ ->
          let com = { Up = 2 }
          upgrade agent id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "第二个聚合应用第一条会导致验证错误的变更"
      <| fun _ ->
          let com = { Up = 3 }

          let f =
              fun _ -> upgrade agent id2 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<ValidateError> f "异常类型有误"
      testCase "第一个聚合应用第二条变更"
      <| fun _ ->
          let com = { Content = "c1" }
          change agent id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "第二个聚合应用第二条变更"
      <| fun _ ->
          let com = { Content = "c1" }
          change agent id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "第一个聚合应用第三条变更"
      <| fun _ ->
          let com = { Content = "c2" }
          change agent id1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "第二个聚合应用第三条变更"
      <| fun _ ->
          let com = { Content = "c2" }
          change agent id2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCaseAsync "并行应用领域变更"
      <| async {
          let coms =
              [ for i in 1..1000 ->
                    { Title = $"t{i}"
                      Content = $"c{i}"
                      Grade = 1 } ]

          do!
              coms
              |> List.map (fun c -> create agent (Guid.NewGuid()) (Guid.NewGuid()) c)
              |> Async.Parallel
              |> Async.Ignore
      } ]
    |> testList "Write"
    |> testSequenced
    |> testLabel "Domain"
