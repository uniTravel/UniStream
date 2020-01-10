namespace UniStream.Domain

open System
open System.Text.Json


module DomainLog =

    type T = { AggId: Guid; Message: string }

    type Logger = { AggType: string; LogFunc: string -> Guid -> string -> byte[] -> unit }

    let logger aggType logFunc =
        { AggType = aggType; LogFunc = logFunc }

    let asBytes (log: T) =
        JsonSerializer.SerializeToUtf8Bytes log

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        JsonSerializer.Deserialize<T> span

    let log logger status aggId traceId format =
        let doAfter s =
            let d = { AggId = aggId; Message = s } |> asBytes
            logger.LogFunc logger.AggType traceId status d
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process aggId traceId format = log this "process" aggId traceId format
        member this.Success aggId traceId format = log this "success" aggId traceId format
        member this.Fail aggId traceId format = log this "fail" aggId traceId format