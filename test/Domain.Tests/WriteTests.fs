module Domain.Tests.Write

open System
open System.Collections.Generic
open Expecto
open UniStream.Domain
open Domain


let buildTest setup =
    [ test "新聚合ID执行创建命令" {
          setup
          <| fun agent _ ->
              async {
                  let com =
                      { Title = "t"
                        Content = "c"
                        Grade = 1 }

                  let id = Guid.NewGuid()
                  let! result = create agent id (Guid.NewGuid()) com
                  Expect.isTrue result.IsSuccess "聚合事件写入流有误"
                  let! agg = get agent id
                  Expect.equal (agg.Title, agg.Content, agg.Grade) ("t", "c", 1) "聚合写入有误"
              }
      }
      test "新聚合ID创建命令存在不合验证规则的情形" {
          setup
          <| fun agent _ ->
              let com =
                  { Title = "t"
                    Content = "c"
                    Grade = 4 }

              Expect.throwsAsyncT<ValidateException>
                  (fun _ ->
                      async {
                          match! create agent (Guid.NewGuid()) (Guid.NewGuid()) com with
                          | Fail ex -> raise ex
                          | _ -> ()
                      })
                  "异常类型有误"
      }
      test "新聚合ID执行更新命令" {
          setup
          <| fun agent _ ->
              Expect.throwsAsyncT<ReadException>
                  (fun _ ->
                      async {
                          match! change agent (Guid.NewGuid()) (Guid.NewGuid()) { Content = "c" } with
                          | Fail ex -> raise ex
                          | _ -> ()
                      })
                  "异常类型有误"
      }
      test "已有聚合执行更新命令" {
          setup
          <| fun agent id ->
              async {
                  let! result = change agent id (Guid.NewGuid()) { Content = "c1" }
                  Expect.isTrue result.IsSuccess "聚合事件写入流有误"
                  let! agg = get agent id
                  Expect.equal (agg.Title, agg.Content, agg.Grade) ("t", "c1", 1) "聚合写入有误"
              }
      }
      test "已有聚合执行更新命令存在不合验证规则的情形" {
          setup
          <| fun agent id ->
              Expect.throwsAsyncT<ValidateException>
                  (fun _ ->
                      async {
                          match! upgrade agent id (Guid.NewGuid()) { Up = 3 } with
                          | Fail ex -> raise ex
                          | _ -> ()
                      })
                  "异常类型有误"
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

        let opt = AggregateOptions(Capacity = 10000)
        let agent = Aggregator.init Note stream opt
        Aggregator.register agent <| Replay<Note, NoteCreated>()
        Aggregator.register agent <| Replay<Note, NoteChanged>()
        Aggregator.register agent <| Replay<Note, NoteUpgraded>()
        let id = Guid.NewGuid()

        let com =
            { Title = "t"
              Content = "c"
              Grade = 1 }

        let _ = create agent id (Guid.NewGuid()) com |> Async.RunSynchronously

        try
            f agent id |> Async.RunSynchronously
        finally
            repo.Clear()
            agent.Dispose()
    |> testList "Write"
    |> testSequenced
    |> testLabel "已注册重播"
