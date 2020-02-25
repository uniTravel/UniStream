namespace UniStream.Domain

open System
open System.Text.Json


module DomainLog =

    type T = { AggType: string; AggId: Guid; Message: string }

    type Logger = { AggType: string; LogFunc: string -> Guid -> string -> byte[] -> unit }

    let logger aggType logFunc =
        { AggType = aggType; LogFunc = logFunc }

    let asBytes (log: T) =
        JsonSerializer.SerializeToUtf8Bytes log

    let log logger cvType status aggId traceId format =
        let doAfter s =
            let d = { AggType = logger.AggType; AggId = aggId; Message = s } |> asBytes
            logger.LogFunc cvType traceId status d
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process cvType aggId traceId format = log this cvType "process" aggId traceId format
        member this.Success cvType aggId traceId format = log this cvType "success" aggId traceId format
        member this.Fail cvType aggId traceId format = log this cvType "fail" aggId traceId format