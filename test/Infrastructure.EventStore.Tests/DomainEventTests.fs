module Infrastructure.EventStore.Tests.DomainEvent

open System
open System.Text
open System.Text.Json
open Expecto


let domainEvent (app: AppService) name =
    let aggType = "Note-"
    let aggId = Guid.NewGuid().ToString()
    let name = "领域事件_" + name
    testSequenced <| testList name [
        testCase "读取一个不存在的流" <| fun _ ->
            let count = app.Reader aggType aggId 0uL |> Async.RunSynchronously |> Seq.length
            Expect.equal count 0 "获取的事件数量错误"
        testCase "向不存在的流写入一个领域事件" <| fun _ ->
            let e1 = Encoding.UTF8.GetBytes "test1" |> ReadOnlyMemory
            seq { "Created", e1, Nullable() } |> app.Writer aggType aggId UInt64.MaxValue |> Async.RunSynchronously
            let count = app.Reader aggType aggId 0uL |> Async.RunSynchronously |> Seq.length
            Expect.equal count 1 "获取的事件数量错误"
        testCase "继续写入两个领域事件" <| fun _ ->
            let e2 = Encoding.UTF8.GetBytes "test2" |> ReadOnlyMemory
            let e3 = Encoding.UTF8.GetBytes "test3" |> ReadOnlyMemory
            seq { "Changed", e2, Nullable(); "Changed", e3, Nullable() } |> app.Writer aggType aggId 0uL |> Async.RunSynchronously
            let count = app.Reader aggType aggId 1uL |> Async.RunSynchronously |> Seq.length
            Expect.equal count 2 "获取的事件数量错误"
        testCase "继续写入一个版本号为1的领域事件" <| fun _ ->
            let e4 = Encoding.UTF8.GetBytes "test4" |> ReadOnlyMemory
            let f = fun _ -> seq { "Changed", e4, Nullable() } |> app.Writer aggType aggId 1uL |> Async.RunSynchronously
            Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
        testCase "继续写入一个版本号为2的领域事件" <| fun _ ->
            let e4 = Encoding.UTF8.GetBytes "test4" |> ReadOnlyMemory
            seq { "Changed", e4, Nullable() } |> app.Writer aggType aggId 2uL |> Async.RunSynchronously
            let count = app.Reader aggType aggId 1uL |> Async.RunSynchronously |> Seq.length
            Expect.equal count 3 "获取的事件数量错误"
        testCase "并行处理" <| fun _ ->
            let r =
                seq { 0 .. 999 }
                |> Seq.map (fun i -> async {
                    let aggId = Guid.NewGuid().ToString()
                    let traceId = Guid.NewGuid().ToString()
                    let! count = app.Reader aggType aggId 0uL
                    let command = JsonSerializer.SerializeToUtf8Bytes { Title = "title"; Content = "initial content" } |> ReadOnlyMemory
                    let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                    do! seq { "NoteCreated", command, metadata } |> app.Writer "Benchmark.Direct.Note-" aggId UInt64.MaxValue
                    let traceId = Guid.NewGuid().ToString()
                    let command = JsonSerializer.SerializeToUtf8Bytes { Content = "changed content" } |> ReadOnlyMemory
                    let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                    do! seq { "NoteChanged", command, metadata } |> app.Writer "Benchmark.Direct.Note-" aggId 0uL })
                |> Async.Parallel
                |> Async.RunSynchronously
            Expect.equal r.Length 1000 ""
    ]
    |> testLabel "EventStore"

[<Tests>]
let defaultTests = domainEvent EventStoreConfig.app "Grpc客户端"