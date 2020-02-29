module Logging.Tests

open System
open System.Text.Json
open Expecto
open UniStream.Domain


type LogLevel =
    | Trace = 0
    | Debug = 1
    | Info = 2
    | Warn = 3
    | Error = 4
    | Critical = 5

[<CLIMutable>]
type DomainLog = { AggType:string; AggId:Guid; TraceId: Guid; Message: string }

[<CLIMutable>]
type DiagnoseLog = { Level: LogLevel; Message: string; StackTrack: string }

let aggId = Guid.NewGuid()
let traceId = Guid.NewGuid()

[<PTests>]
let domainLog =
    testSequenced <| testList "领域日志" [
        let withArgs f () =
            let ldFunc cvType status dLog =
                let span = ReadOnlySpan dLog
                let r = JsonSerializer.Deserialize<DomainLog> span
                printfn "%s-%s：%s|%A|%A|%s" cvType status r.AggType r.AggId r.TraceId r.Message
            let ld = DomainLog.logger "Note" ldFunc
            go "领域日志" |> f ld aggId traceId
        yield! testFixture withArgs [
            "Process", fun ld aggId traceId finish ->
                ld.Process "CreateNote" aggId traceId "开始"
                finish 1
            "Success", fun ld aggId traceId finish ->
                ld.Success "CreateNote" aggId traceId "完成"
                finish 2
            "Fail", fun ld aggId traceId finish ->
                ld.Fail "CreateNote" aggId traceId "错误"
                finish 3
        ]
    ]
    |> testLabel "UniStream.Domain"

[<PTests>]
let diagnoseLog =
    testSequenced <| testList "诊断日志" [
        let withArgs f () =
            let lgFunc aggType gLog =
                let span = ReadOnlySpan gLog
                let r = JsonSerializer.Deserialize<DiagnoseLog> span
                printfn "%s-%A：%s|%s" aggType r.Level r.Message r.StackTrack
            let lg = DiagnoseLog.logger "Note" lgFunc
            go "诊断日志" |> f lg
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
    |> testLabel "UniStream.Domain"