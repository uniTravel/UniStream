module Domain.Tests.Read

open System
open System.Collections.Generic
open Expecto
open UniStream.Domain
open Domain


let buildTest setup =
    [ test "聚合缓存有" {
          setup
          <| fun agent _ id2 ->
              async {
                  let! agg = get agent id2
                  Expect.equal (agg.Title, agg.Content, agg.Grade) ("t", "c6", 1) "获取聚合有误"
              }
      }
      test "聚合缓存无，仓储有" {
          setup
          <| fun agent id1 _ ->
              async {
                  let! agg = get agent id1
                  Expect.equal (agg.Title, agg.Content, agg.Grade) ("t", "c1", 2) "获取聚合有误"
              }
      }
      test "仓储无" {
          setup
          <| fun agent _ _ ->
              Expect.throwsAsyncT<ReadException> (fun _ -> get agent (Guid.NewGuid()) |> Async.Ignore) "异常类型有误"
      } ]

[<Tests>]
let test =
    buildTest
    <| fun f ->
        let repo = Dictionary<string, (uint64 * string * ReadOnlyMemory<byte>) list>(10000)

        let stream =
            { new IStream<Note> with
                member _.Writer = writer repo "Note"
                member _.Reader = reader repo "Note"
                member _.Restore = restore "Note" }

        let opt = AggregateOptions(Capacity = 3)
        let agent = Aggregator.init Note stream opt
        Aggregator.register agent <| Replay<Note, NoteCreated>()
        Aggregator.register agent <| Replay<Note, NoteChanged>()
        Aggregator.register agent <| Replay<Note, NoteUpgraded>()
        let id1 = Guid.NewGuid()
        let id2 = Guid.NewGuid()

        let com =
            { Title = "t"
              Content = "c"
              Grade = 1 }

        // id1执行完整的命令，id2仅执行创建命令
        let _ = create agent id1 (Guid.NewGuid()) com |> Async.RunSynchronously
        let _ = create agent id2 (Guid.NewGuid()) com |> Async.RunSynchronously
        let com = { Content = "c1" }
        let _ = change agent id1 (Guid.NewGuid()) com |> Async.RunSynchronously
        let com = { Up = 1 }
        let _ = upgrade agent id1 (Guid.NewGuid()) com |> Async.RunSynchronously

        // 操作记录总数到10后触发聚合缓存清理，清理的结果：
        // 1、仓储有id1、id2两个聚合，但聚合缓存只有id2。
        // 2、聚合及命令的操作记录数变为1。
        [ for i in 1..6 -> { Content = $"c{i}" } ]
        |> List.iter (fun c -> change agent id2 (Guid.NewGuid()) c |> Async.RunSynchronously |> ignore)

        try
            f agent id1 id2 |> Async.RunSynchronously
        finally
            repo.Clear()
            agent.Dispose()
    |> testList "Read"
    |> testLabel "已注册重播"
