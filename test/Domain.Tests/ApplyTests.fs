module Domain.Tests.Apply

open System
open System.Collections.Generic
open Expecto
open UniStream.Domain
open Domain.Models


let dic = Dictionary<string, seq<uint64 * string * ReadOnlyMemory<byte>>>(10000)
let ids = Array.zeroCreate<Guid> 2

let writer stream revision evType ev =
    async {
        if dic.ContainsKey stream then
            dic[stream] <- Seq.append dic[stream] [ revision + 1UL, evType, ev ]
        else
            dic.Add(stream, Seq.append Seq.empty [ revision + 1UL, evType, ev ])
    }

let reader stream revision =
    async {
        if dic.ContainsKey stream then
            let evs =
                dic[stream]
                |> Seq.skipWhile (fun (v, _, _) -> v < revision)
                |> Seq.map (fun (v, et, e) -> (et, e))

            return evs
        else
            return failwith $"The key {stream} is wrong."
    }

let agent = Aggregator.build Note writer reader 10000 1.0
let init = Aggregator.init agent
let apply = Aggregator.apply agent


[<Tests>]
let test =
    testSequenced
    <| testList
        "Apply"
        [ testCase "初始生成两个聚合"
          <| fun _ ->
              let cm: CreateNote = { Title = "title"; Content = "content" }

              let agg: Note =
                  Delta.serialize cm |> init (nameof CreateNote) |> Async.RunSynchronously

              ids[0] <- agg.Id
              Expect.equal (agg.Revision, agg.Title, agg.Content) (0UL, "title", "content") ""
              let agg = Delta.serialize cm |> init (nameof CreateNote) |> Async.RunSynchronously
              ids[1] <- agg.Id
              Expect.equal (agg.Revision, agg.Title, agg.Content) (0UL, "title", "content") ""
              Expect.hasLength dic 2 ""
          testCase "第一个聚合，再应用两条命令"
          <| fun _ ->
              let cm1: ChangeNote = { Content = "content1" }

              let agg =
                  Delta.serialize cm1
                  |> apply ids[0] 0UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 1UL, "title", "content1") ""
              let cm2: ChangeNote = { Content = "content2" }

              let agg =
                  Delta.serialize cm2
                  |> apply ids[0] 1UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 2UL, "title", "content2") ""
              Expect.hasLength dic 2 ""
          testCase "第二个聚合，再应用一条命令"
          <| fun _ ->
              let cm1: ChangeNote = { Content = "content1" }

              let agg =
                  Delta.serialize cm1
                  |> apply ids[1] 0UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[1], 1UL, "title", "content1") ""
              Expect.hasLength dic 2 ""
          testCaseAsync "并行应用领域命令"
          <| async {
              let cms: CreateNote list =
                  [ for i in 1..1000 ->
                        { Title = $"title{i}"
                          Content = $"content{i}" } ]

              let! r =
                  cms
                  |> List.map (fun c -> Delta.serialize c |> init (nameof CreateNote))
                  |> Async.Parallel

              Expect.allEqual (r |> Array.map (fun n -> n.Revision)) 0UL ""
              Expect.hasLength dic 1002 ""
              Expect.hasLength r 1000 ""
          }
          testCase "第一个聚合，命令传入的版本为0"
          <| fun _ ->
              let cm: ChangeNote = { Content = "content3" }

              let f =
                  fun _ ->
                      Delta.serialize cm
                      |> apply ids[0] 0UL (nameof ChangeNote)
                      |> Async.Ignore
                      |> Async.RunSynchronously

              Expect.throwsC f (fun ex -> printfn $"%s{ex.Message}")
          testCase "第一个聚合，命令传入的版本为1"
          <| fun _ ->
              let cm: ChangeNote = { Content = "content3" }

              let f =
                  fun _ ->
                      Delta.serialize cm
                      |> apply ids[0] 1UL (nameof ChangeNote)
                      |> Async.Ignore
                      |> Async.RunSynchronously

              Expect.throwsC f (fun ex -> printfn $"%s{ex.Message}")
          testCase "缓存刷新后，第一个聚合，命令传入的版本为3"
          <| fun _ ->
              Threading.Thread.Sleep 2000
              let cm: ChangeNote = { Content = "content3" }

              let f =
                  fun _ ->
                      Delta.serialize cm
                      |> apply ids[0] 3UL (nameof ChangeNote)
                      |> Async.Ignore
                      |> Async.RunSynchronously

              Expect.throwsC f (fun ex -> printfn $"%s{ex.Message}")
          testCase "缓存刷新后，第一个聚合，命令传入的版本为2"
          <| fun _ ->
              let cm: ChangeNote = { Content = "content3" }

              let agg =
                  Delta.serialize cm
                  |> apply ids[0] 2UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 3UL, "title", "content3") "" ]
    |> testLabel "Domain"
