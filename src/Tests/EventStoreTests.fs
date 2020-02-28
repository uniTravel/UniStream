module EventStore.Tests

open System
open System.Text
open Microsoft.FSharp.Core.Operators
open Expecto
open UniStream.Infrastructure

let esAdmin = Uri "tcp://admin:changeit@localhost:4011"
let esOps = Uri "tcp://ops:changeit@localhost:4011"
let ldAdmin = Uri "tcp://admin:changeit@localhost:4012"
let lgAdmin = Uri "tcp://admin:changeit@localhost:4013"
let admin = DomainEvent.create esAdmin
let ops = DomainEvent.create esOps
let ld = DomainLog.create ldAdmin
let handler sub eventId eventType data = async {
    printfn "%s：%A，%s" sub eventId eventType
}


[<Tests>]
let domainEventTests =
    let aggType = "Note"
    let evt1 = "NoteCreated"
    let evt2 = "NoteChanged"
    let evt3 = "NoteCleaned"
    let aggId = Guid.NewGuid()
    testSequenced <| testList "EventStore DomainEvent" [
        let withArgs f () =
            let writer = DomainEvent.write ops
            go "EventStore DomainEvent" |> f writer
        yield! testFixture withArgs [
            "客户端订阅", fun writer finish ->
                DomainEvent.subscribeToStream admin "NoteChanged" <| handler "单点订阅"
                finish 1
            "服务端订阅", fun writer finish ->
                DomainEvent.connectSubscription admin "NoteChanged" "Group" <| handler "群组订阅"
                finish 2
            "创建Note", fun writer finish ->
                let d1 = Encoding.UTF8.GetBytes "Initial Note"
                let version = writer aggType aggId 0L [| (evt1, d1) |]
                Expect.equal version 0L "返回版本号错误。"
                finish 3
            "更改Note", fun writer finish ->
                let d1 = Encoding.UTF8.GetBytes "Changed Note"
                let version = writer aggType aggId 1L [| (evt2, d1) |]
                Expect.equal version 1L "返回版本号错误。"
                finish 4
            "清理Note", fun writer finish ->
                let d1 = Encoding.UTF8.GetBytes "Changed Note"
                let d2 = Encoding.UTF8.GetBytes "Cleaned Note"
                let version = writer aggType aggId 2L [| (evt2, d1); (evt3, d2) |]
                Expect.equal version 3L "返回版本号错误。"
                finish 5
            "等待事件处理程序完成", fun writer finish ->
                Threading.Thread.Sleep 100
                finish 0
        ]
    ]
    |> testLabel "UniStream.Infrastructure"

[<Tests>]
let domainLogTests =
    let cvType = "Note.Domain.Actor"
    testSequenced <| testList "EventStore DomainLog" [
        let withArgs f () =
            let writer = DomainLog.write ld
            go "EventStore DomainLog" |> f writer
        yield! testFixture withArgs [
            "开始", fun writer finish ->
                let dLog = Encoding.UTF8.GetBytes "Initial Note"
                writer cvType "process" dLog
                finish 1
            "处理中", fun writer finish ->
                let dLog = Encoding.UTF8.GetBytes "Change Note"
                writer cvType "success" dLog
                finish 2
        ]
    ]
    |> testLabel "UniStream.Infrastructure"