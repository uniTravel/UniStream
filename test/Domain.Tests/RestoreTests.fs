module Domain.Tests.Restore

open System
open System.Collections.Generic
open Expecto
open UniStream.Domain
open Domain


let buildTest setup =
    [ test "提交命令操作缓存中有记录的命令" {
          setup
          <| fun agent id ->
              async {
                  let com =
                      { Title = "t"
                        Content = "c"
                        Grade = 1 }

                  let! result = create agent id restored[0] com
                  Expect.isTrue result.IsDuplicate "命令操作记录缓存有误"
                  let! result = create agent id restored[1] com
                  Expect.isTrue result.IsDuplicate "命令操作记录缓存有误"
                  let! result = create agent id restored[2] com
                  Expect.equal result Duplicate "命令操作记录缓存有误"
              }
      }
      test "重复提交命令操作缓存中没有记录的命令" {
          setup
          <| fun agent id ->
              async {
                  let com =
                      { Title = "t"
                        Content = "c"
                        Grade = 1 }

                  let comId = Guid.NewGuid()
                  let! result = create agent id comId com
                  Expect.equal result Success "命令操作记录缓存有误"
                  let! result = create agent id comId com
                  Expect.equal result Duplicate "命令操作记录缓存有误"
              }
      } ]

[<Tests>]
let test =
    buildTest
    <| fun f ->
        let repo = Dictionary<string, (uint64 * string * ReadOnlyMemory<byte>) list> 10000

        let stream =
            { new IStream<Note> with
                member _.Writer = writer repo "Note"
                member _.Reader = reader repo "Note"
                member _.Restore = restore "Note" }

        let opt = AggregateOptions(Capacity = 3)
        let agent = Aggregator.init Note cts.Token stream opt
        let id = Guid.NewGuid()

        try
            f agent id |> Async.RunSynchronously
        finally
            repo.Clear()
            agent.Dispose()
    |> testList "未注册重播"
    |> testLabel "Restore"
