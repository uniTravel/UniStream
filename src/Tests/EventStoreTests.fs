module EventStore.Tests

open System
open System.Text
open Microsoft.FSharp.Core.Operators
open Expecto
open UniStream.Infrastructure

let uriAdmin = Uri "tcp://admin:changeit@localhost:4011"
let uriOps = Uri "tcp://ops:changeit@localhost:4011"
let admin = DomainEvent.create uriAdmin
let ops = DomainEvent.create uriOps
let handler sub eventId eventType data = async {
    printfn "%s：%A，%s" sub eventId eventType
}
let aggType = "Note"


[<Tests>]
let adminTests =
    let aggId = Guid.NewGuid()
    testSequenced <| testList "EventStore Admin" [
        let withArgs f () =
            let writer = DomainEvent.write admin
            go "EventStore Admin" |> f writer
        yield! testFixture withArgs [
            "客户端订阅", fun writer finish ->
                DomainEvent.subscribeToStream admin "NoteChanged" <| handler "单点订阅Admin"
                finish 1
            "连接到服务端订阅", fun writer finish ->
                DomainEvent.connectSubscription admin "NoteChanged" "Group" <| handler "群组订阅"
                finish 2
            "创建Note", fun writer finish ->
                let traceId = Guid.NewGuid()
                let deltaType = "NoteCreated"
                let delta = Encoding.UTF8.GetBytes "Initial Note"
                writer aggType aggId 0L traceId deltaType delta
                finish 3
            "更改Note", fun writer finish ->
                let traceId = Guid.NewGuid()
                let deltaType = "NoteChanged"
                let delta = Encoding.UTF8.GetBytes "Changed Note"
                writer aggType aggId 1L traceId deltaType delta
                finish 4
            "等待事件处理程序完成", fun writer finish ->
                Threading.Thread.Sleep 100
                finish 0
        ]
    ]
    |> testLabel "UniStream.Infrastructure"


[<Tests>]
let opsTests =
    let aggId = Guid.NewGuid()
    testSequenced <| testList "EventStore Ops" [
        let withArgs f () =
            let writer = DomainEvent.write ops
            go "EventStore Ops" |> f writer
        yield! testFixture withArgs [
            "客户端订阅", fun writer finish ->
                DomainEvent.subscribeToStream ops "NoteChanged" <| handler "单点订阅Ops"
                finish 1
            "连接到服务端订阅", fun writer finish ->
                DomainEvent.connectSubscription ops "NoteChanged" "Group" <| handler "群组订阅"
                finish 2
            "创建Note", fun writer finish ->
                let traceId = Guid.NewGuid()
                let deltaType = "NoteCreated"
                let delta = Encoding.UTF8.GetBytes "Initial Note"
                writer aggType aggId 0L traceId deltaType delta
                finish 3
            "更改Note", fun writer finish ->
                let traceId = Guid.NewGuid()
                let deltaType = "NoteChanged"
                let delta = Encoding.UTF8.GetBytes "Changed Note"
                writer aggType aggId 1L traceId deltaType delta
                finish 4
            "等待事件处理程序完成", fun writer finish ->
                Threading.Thread.Sleep 100
                finish 0
        ]
    ]
    |> testLabel "UniStream.Infrastructure"