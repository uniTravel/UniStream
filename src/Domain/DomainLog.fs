namespace UniStream.Domain

open System.Text.Json


module DomainLog =

    type Logger = { AggType: string; LogFunc: string -> string -> byte[] -> unit }

    let logger aggType logFunc =
        { AggType = aggType; LogFunc = logFunc }

    let log logger cvType status (aggId: string) (traceId: string) format =
        let doAfter (s: string) =
            {| AggType = logger.AggType; AggId = aggId; TraceId = traceId; Message = s |}
            |> JsonSerializer.SerializeToUtf8Bytes
            |> logger.LogFunc cvType status
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process cvType aggId traceId format = log this cvType "process" aggId traceId format
        member this.Success cvType aggId traceId format = log this cvType "success" aggId traceId format
        member this.Fail cvType aggId traceId format = log this cvType "fail" aggId traceId format