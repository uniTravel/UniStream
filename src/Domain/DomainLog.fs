namespace UniStream.Domain

open System
open System.Text


module DomainLog =

    type T = { AggId: Guid; Message: string }

    type Logger = { AggType: string; LogFunc: string -> Guid -> string -> byte[] -> unit }

    let logger aggType logFunc =
        { AggType = aggType; LogFunc = logFunc }

    let asBytes log =
        let array = Array.zeroCreate<byte> <| 16 + log.Message.Length
        let span = Span array
        let aId = span.Slice (0, 16)
        let msg = span.Slice (16, log.Message.Length)
        ((log.AggId.ToByteArray()).AsSpan()).CopyTo aId
        ((Encoding.UTF8.GetBytes log.Message).AsSpan()).CopyTo msg
        array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let aId = Guid (span.Slice (0, 16))
        let msg = Encoding.UTF8.GetString (span.Slice (16, bytes.Length - 16))
        { AggId = aId; Message = msg }

    let log logger status aggId traceId format =
        let doAfter s =
            let d = { AggId = aggId; Message = s } |> asBytes
            logger.LogFunc logger.AggType traceId status d
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process aggId traceId format = log this "process" aggId traceId format
        member this.Success aggId traceId format = log this "success" aggId traceId format
        member this.Fail aggId traceId format = log this "fail" aggId traceId format