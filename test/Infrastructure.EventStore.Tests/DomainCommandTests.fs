module Infrastructure.EventStore.Tests.DomainCommand

open System
open System.Text.Json
open Expecto


let handler traceId eType (data: ReadOnlyMemory<byte>) callback = async {
    let note = JsonSerializer.Deserialize<CreateNote> data.Span
    let note = { Title = note.Title; Content = note.Content }
    callback note }

let subscriber (app: AppService) = async {
    app.SubscribeCreateNote handler |> Async.RunSynchronously |> ignore }

let domainCommand (app: AppService) name =
    let aggId = Guid.NewGuid().ToString()
    let name = "领域命令_" + name
    Async.Start <| subscriber app
    testSequenced <| testList name [
        testCase "向不存在的流写入一个领域命令" <| fun _ ->
            let n1 : CreateNote = { Title = "title1"; Content = "content1"}
            match app.LaunchCreateNote aggId n1 |> Async.RunSynchronously with
            | Ok result -> Expect.equal result.Content n1.Content "返回结果错误"
            | Error err -> failtestf "测试出错：%s" err
        testCase "向流写入第二个领域命令" <| fun _ ->
            let n2 : CreateNote = { Title = "title2"; Content = "content2"}
            match app.LaunchCreateNote aggId n2 |> Async.RunSynchronously with
            | Ok result -> Expect.equal result.Content n2.Content "返回结果错误"
            | Error err -> failtestf "测试出错：%s" err
    ]
    |> testLabel "EventStore"


[<Tests>]
let defaultTests = domainCommand EventStoreConfig.app "Grpc客户端"