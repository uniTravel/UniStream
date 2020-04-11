module EventStore.Tests

open System
open System.Text
open Microsoft.FSharp.Core.Operators
open Expecto
open EventStore.ClientAPI
open UniStream.Infrastructure
open UniStream.Domain


type LogLevel =
    | Trace = 0
    | Debug = 1
    | Info = 2
    | Warn = 3
    | Error = 4
    | Critical = 5

let connect (uri: Uri) =
    let conn = EventStoreConnection.Create uri
    conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
    conn

let esAdmin = Uri "tcp://admin:changeit@localhost:4011"
let esOps = Uri "tcp://ops:changeit@localhost:4011"
let ldAdmin = Uri "tcp://admin:changeit@localhost:4012"
let lgAdmin = Uri "tcp://admin:changeit@localhost:4013"
let csAdmin = Uri "tcp://admin:changeit@localhost:4016"
let admin = connect esAdmin
let ops = connect esOps
let ld = connect ldAdmin
let lg = connect lgAdmin
let cs = connect csAdmin
let handler sub aggId eventType version data metadata = async {
    printfn "%s：%A，%s, %d" sub aggId eventType version
}


[<Tests>]
let domainEventTests =
    let aggType = "Note"
    let evt1 = "NoteCreated"
    let evt2 = "NoteChanged"
    let evt3 = "NoteCleaned"
    let aggId = Guid.NewGuid().ToString()
    let traceId = Guid.NewGuid().ToString()
    testSequenced <| testList "EventStore DomainEvent" [
        let withArgs f () =
            let writer = DomainEvent.write ops
            go "EventStore DomainEvent" |> f writer
        yield! testFixture withArgs [
            "客户端订阅", fun writer finish ->
                let unsub = DomainEvent.subscribe admin "$et-NoteChanged" <| handler "单点订阅"
                finish 1
            // "服务端订阅", fun writer finish ->
            //     DomainEvent.connectSubscription admin "$et-NoteChanged" "Group" <| handler "群组订阅"
            //     finish 2
            "创建Note", fun writer finish ->
                let d1 = Encoding.UTF8.GetBytes "Initial Note"
                let metadata = MetaData.correlationId traceId
                let version = writer aggType aggId 0L [| (evt1, d1, metadata) |] |> Async.RunSynchronously
                Expect.equal version 0L "返回版本号错误。"
                finish 3
            "更改Note", fun writer finish ->
                let d1 = Encoding.UTF8.GetBytes "Changed Note"
                let metadata = MetaData.correlationId traceId
                let version = writer aggType aggId 1L [| (evt2, d1, metadata) |] |> Async.RunSynchronously
                Expect.equal version 1L "返回版本号错误。"
                finish 4
            "清理Note", fun writer finish ->
                let d1 = Encoding.UTF8.GetBytes "Changed Note"
                let d2 = Encoding.UTF8.GetBytes "Cleaned Note"
                let metadata = MetaData.correlationId traceId
                let version = writer aggType aggId 2L [| (evt2, d1, metadata); (evt3, d2, metadata) |] |> Async.RunSynchronously
                Expect.equal version 3L "返回版本号错误。"
                finish 5
            "获取全部事件流", fun writer finish ->
                let r, v = DomainEvent.get ops aggType aggId 0L
                Expect.hasLength r 4 "集合长度错误。"
                Expect.equal v 3L "返回版本号错误。"
                finish 6
            "获取部分事件流", fun writer finish ->
                let r, v = DomainEvent.get ops aggType aggId 2L
                Expect.hasLength r 2 "集合长度错误。"
                Expect.equal v 3L "返回版本号错误。"
                finish 7
            "等待事件处理程序完成", fun writer finish ->
                Threading.Thread.Sleep 100
                finish 0
        ]
    ]
    |> testLabel "UniStream.Infrastructure"

[<Tests>]
let domainLogTests =
    let user = "test"
    let cvType = "CreateNote"
    let aggId = Guid.NewGuid().ToString()
    let traceId = Guid.NewGuid().ToString()
    testSequenced <| testList "EventStore DomainLog" [
        let withArgs f () =
            let writer = DomainLog.write "NoteApp" ld
            let ld = DomainLog.logger "Note" writer
            go "EventStore DomainLog" |> f ld
        yield! testFixture withArgs [
            "开始", fun ld finish ->
                ld.Process user cvType aggId traceId "开始。"
                finish 1
            "处理中", fun ld finish ->
                ld.Process user cvType aggId traceId "应用命令成功。"
                finish 2
            "完成", fun ld finish ->
                ld.Success user cvType aggId traceId "成功。"
                finish 3
            "失败", fun ld finish ->
                ld.Fail user cvType aggId traceId "失败。"
                finish 3
        ]
    ]
    |> testLabel "UniStream.Infrastructure"

[<Tests>]
let diagnoseLogTests =
    testSequenced <| testList "EventStore DiagnoseLog" [
        let withArgs f () =
            let writer = DiagnoseLog.write "NoteApp" lg
            let lg = DiagnoseLog.logger "Note" writer
            go "EventStore DiagnoseLog" |> f lg
        yield! testFixture withArgs [
            "Trace", fun lg finish ->
                lg.Trace "TRACE：%s" "跟踪"
                finish 1
            "Debug", fun lg finish ->
                lg.Debug "DEBUG：%s" "调试"
                finish 2
            "Info", fun lg finish ->
                lg.Info "INFO：%s" "信息"
                finish 3
            "Warn", fun lg finish ->
                lg.Warn "WARN：%s" "告警"
                finish 4
            "Error", fun lg finish ->
                lg.Error "堆栈信息" "ERROR：%s" "错误"
                finish 5
            "Critical", fun lg finish ->
                lg.Critical "堆栈信息" "CRITICAL：%s" "致命错误"
                finish 6
        ]
    ]
    |> testLabel "UniStream.Infrastructure"

[<Tests>]
let domainCommandTests =
    let cvType = "CreateNote"
    testSequenced <| testList "EventStore DomainCommand" [
        let withArgs f () =
            let writer = DomainCommand.write cs
            go "EventStore DomainCommand" |> f writer
        yield! testFixture withArgs [
            "命令1", fun writer finish ->
                let traceId = Guid.NewGuid().ToString()
                let aggId = Guid.NewGuid()
                let data = Encoding.UTF8.GetBytes "Initial Note"
                writer cvType aggId data <| MetaData.correlationId traceId |> Async.RunSynchronously
                finish 1
            "命令2", fun writer finish ->
                let traceId = Guid.NewGuid().ToString()
                let aggId = Guid.NewGuid()
                let data = Encoding.UTF8.GetBytes "Change Note"
                writer cvType aggId data <| MetaData.correlationId traceId |> Async.RunSynchronously
                finish 2
        ]
    ]
    |> testLabel "UniStream.Infrastructure"