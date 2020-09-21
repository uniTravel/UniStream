module Infrastructure.EventStore.Tests.DomainCommand

open System
open System.Text.Json
open Expecto
open UniStream.Infrastructure.EventStore


let domainCommand (app: AppService) name =
    let name = "领域命令_" + name
    let launch aggId idx = async {
        let note : CreateNote = { Title = "title" + idx.ToString(); Content = "content" + idx.ToString() }
        let! result = app.CreateNote "Test" aggId note
        Expect.equal result.Content ("content" + idx.ToString()) "返回结果错误" }
    let aggId = Guid.NewGuid().ToString()
    testSequenced <| testList name [
        testCase "向不存在的流写入一个领域命令" <| fun _ ->
            let subscriber = app.CommandSubscriber
            CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
            <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                let note = JsonSerializer.Deserialize<CreateNote> data.Span
                let note = { Title = note.Title; Content = note.Content }
                callback note }
            |> Async.RunSynchronously
            launch aggId 1 |> Async.RunSynchronously
            CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
        testCase "向流写入第二个领域命令" <| fun _ ->
            let subscriber = app.CommandSubscriber
            CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
            <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                let note = JsonSerializer.Deserialize<CreateNote> data.Span
                let note = { Title = note.Title; Content = note.Content }
                callback note }
            |> Async.RunSynchronously
            launch aggId 2 |> Async.RunSynchronously
            CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
    ]
    |> testLabel "EventStore"

[<Tests>]
let defaultTests = domainCommand app "Grpc客户端"