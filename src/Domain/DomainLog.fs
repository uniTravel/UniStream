namespace UniStream.Domain

open System.Text.Json


module DomainLog =

    type Logger = { AggType: string; LogFunc: string -> string -> byte[] -> byte[] -> unit }

    let logger aggType logFunc =
        { AggType = aggType; LogFunc = logFunc }

    let log logger user cvType status (aggId: string) (traceId: string) format =
        let doAfter (s: string) =
            let data = {| AggType = logger.AggType; AggId = aggId; Status = status; Message = s |} |> JsonSerializer.SerializeToUtf8Bytes
            logger.LogFunc user cvType data <| MetaData.correlationId traceId
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process user cvType aggId traceId format = log this user cvType "process" aggId traceId format
        member this.Success user cvType aggId traceId format = log this user cvType "success" aggId traceId format
        member this.Fail user cvType aggId traceId format = log this user cvType "fail" aggId traceId format