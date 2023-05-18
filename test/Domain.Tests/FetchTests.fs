module Domain.Tests.Fetch

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

let fetcher stream start maxCount =
    async {
        if dic.ContainsKey stream then
            let evs =
                dic[stream]
                |> Seq.skip (int start)
                |> Seq.truncate (int maxCount)
                |> Seq.map (fun (v, et, e) -> (et, e))

            return evs
        else
            return failwith $"The key {stream} is wrong."
    }

let agent1 = Aggregator.build Note writer reader 10000 1.0
let init = Aggregator.init agent1
let apply = Aggregator.apply agent1

let agent2 = Fetcher.build Note fetcher 10000 1.0
let get = Fetcher.get agent2


[<Tests>]
let tests =
    testSequenced
    <| testList
        "Fetch"
        [ testCase "初始生成两个聚合"
          <| fun _ ->
              let cm: CreateNote = { Title = "title"; Content = "content" }
              let agg = Delta.serialize cm |> init (nameof CreateNote) |> Async.RunSynchronously
              ids[0] <- agg.Id
              Expect.equal (agg.Revision, agg.Title, agg.Content) (0UL, "title", "content") ""
              let agg = Delta.serialize cm |> init (nameof CreateNote) |> Async.RunSynchronously
              ids[1] <- agg.Id
              Expect.equal (agg.Revision, agg.Title, agg.Content) (0UL, "title", "content") ""
          testCase "第一个聚合，再应用三条命令"
          <| fun _ ->
              let cm1: ChangeNote = { Content = "content11" }

              let agg =
                  Delta.serialize cm1
                  |> apply ids[0] 0UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 1UL, "title", "content11") ""
              let cm2: ChangeNote = { Content = "content12" }

              let agg =
                  Delta.serialize cm2
                  |> apply ids[0] 1UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 2UL, "title", "content12") ""
              let cm3: ChangeNote = { Content = "content13" }

              let agg =
                  Delta.serialize cm3
                  |> apply ids[0] 2UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 3UL, "title", "content13") ""
          testCase "第二个聚合，再应用一条命令"
          <| fun _ ->
              let cm1: ChangeNote = { Content = "content21" }

              let agg =
                  Delta.serialize cm1
                  |> apply ids[1] 0UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[1], 1UL, "title", "content21") ""
          testCase "第一个聚合，抓取版本2"
          <| fun _ ->
              let agg = get ids[0] 2UL |> Async.RunSynchronously
              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 2UL, "title", "content12") ""
          testCase "第一个聚合，抓取版本1"
          <| fun _ ->
              let agg = get ids[0] 1UL |> Async.RunSynchronously
              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 1UL, "title", "content11") ""
          testCase "第一个聚合，抓取版本3"
          <| fun _ ->
              let agg = get ids[0] 3UL |> Async.RunSynchronously
              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 3UL, "title", "content13") ""
          testCase "第一个聚合，抓取版本4"
          <| fun _ ->
              let f = fun _ -> get ids[0] 4UL |> Async.Ignore |> Async.RunSynchronously
              Expect.throwsC f <| fun ex -> printfn $"{ex.Message}"
          testCase "缓存刷新后，第一个聚合，抓取版本1"
          <| fun _ ->
              Threading.Thread.Sleep 2000
              let agg = get ids[0] 1UL |> Async.RunSynchronously
              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 1UL, "title", "content11") ""
          testCase "缓存刷新后，第一个聚合，抓取版本3"
          <| fun _ ->
              let agg = get ids[0] 3UL |> Async.RunSynchronously
              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[0], 3UL, "title", "content13") ""
          testCase "第二个聚合，抓取版本1"
          <| fun _ ->
              let agg = get ids[1] 1UL |> Async.RunSynchronously
              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[1], 1UL, "title", "content21") ""
          testCase "第二个聚合，抓取版本3"
          <| fun _ ->
              let f = fun _ -> get ids[1] 3UL |> Async.Ignore |> Async.RunSynchronously
              Expect.throwsC f <| fun ex -> printfn $"{ex.Message}"
          testCase "第二个聚合，再应用三条命令"
          <| fun _ ->
              let cm2: ChangeNote = { Content = "content22" }

              let agg =
                  Delta.serialize cm2
                  |> apply ids[1] 1UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[1], 2UL, "title", "content22") ""
              let cm3: ChangeNote = { Content = "content23" }

              let agg =
                  Delta.serialize cm3
                  |> apply ids[1] 2UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[1], 3UL, "title", "content23") ""
              let cm4: ChangeNote = { Content = "content24" }

              let agg =
                  Delta.serialize cm4
                  |> apply ids[1] 3UL (nameof ChangeNote)
                  |> Async.RunSynchronously

              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[1], 4UL, "title", "content24") ""
          testCase "第二个聚合，再次抓取版本3"
          <| fun _ ->
              let agg = get ids[1] 3UL |> Async.RunSynchronously
              Expect.equal (agg.Id, agg.Revision, agg.Title, agg.Content) (ids[1], 3UL, "title", "content23") "" ]
    |> testLabel "Domain"
