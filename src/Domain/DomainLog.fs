namespace UniStream.Domain

open System
open System.Text.Json


module DomainLog =

    type Logger = { AggType: string; LogFunc: string -> string -> ReadOnlyMemory<byte> -> Async<unit> }

    let logger aggType logFunc =
        { AggType = aggType; LogFunc = logFunc }

    let log logger user cvType (status: string) (aggKey: string) (traceId: string) format =
        let doAfter (s: string) =
            let data =
                {| AggType = logger.AggType; AggId = aggKey; TraceId = traceId; Status = status; Message = s |}
                |> JsonSerializer.SerializeToUtf8Bytes |> ReadOnlyMemory
            logger.LogFunc user cvType data |> Async.Start
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process user cvType aggKey traceId format = log this user cvType "process" aggKey traceId format
        member this.Success user cvType aggKey traceId format = log this user cvType "success" aggKey traceId format
        member this.Fail user cvType aggKey traceId format = log this user cvType "fail" aggKey traceId format