module Domain.Tests.Init

open System
open System.Collections.Generic
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

          create agent i1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "创建第二个聚合"
      <| fun _ ->
          let com =
              { Title = "t"
                Content = "c"
                Grade = 1 }

          create agent i2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "第二个聚合持续应用八条变更，以便触发缩容"
      <| fun _ ->
          let c = [ for i in 1..8 -> { Content = $"c{i}" } ]

          c
          |> List.iter (fun c -> change agent i2 (Guid.NewGuid()) c |> Async.RunSynchronously)

      testCase "第一个聚合应用第一条变更"
      <| fun _ ->
          let com = { Content = "c1" }

          let f =
              fun _ -> change agent i1 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册有关重播，然后第一个聚合再次应用第一条变更"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, NoteCreated>()
          Aggregator.register agent <| Replay<Note, NoteChanged>()
          let com = { Content = "c1" }
          change agent i1 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "第二个聚合应用第九条未注册重播的变更"
      <| fun _ ->
          let com = { Up = 1 }
          upgrade agent i2 (Guid.NewGuid()) com |> Async.RunSynchronously
      testCase "第一个聚合持续应用八条变更，以便触发缩容"
      <| fun _ ->
          let c = [ for i in 1..8 -> { Content = $"c{i}" } ]

          c
          |> List.iter (fun c -> change agent i1 (Guid.NewGuid()) c |> Async.RunSynchronously)

      testCase "第二个聚合应用第十条变更"
      <| fun _ ->
          let com = { Content = "c2" }

          let f =
              fun _ -> change agent i2 (Guid.NewGuid()) com |> Async.RunSynchronously |> ignore

          Expect.throwsT<KeyNotFoundException> f "异常类型有误"
      testCase "注册其余重播，然后第二个聚合再次应用第十条变更"
      <| fun _ ->
          Aggregator.register agent <| Replay<Note, NoteUpgraded>()
          let com = { Content = "c2" }
          change agent i2 (Guid.NewGuid()) com |> Async.RunSynchronously ]
    |> testList "Init"
    |> testSequenced
    |> testLabel "Domain"
