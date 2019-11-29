namespace UniStream.Domain

open System
open System.Text
open UniStream.Abstract


module DomainLog =

    type Status =
        | Processing = 0
        | Successed = 1
        | Failed = 2

    type T = { Status: Status; Message: string }

    type Logger = { Name: string; LogFunc: string -> Guid -> string -> byte[] -> byte[] -> unit }

    let logger<'agg when 'agg :> IAggregate> logFunc =
        let name = typeof<'agg>.ToString ()
        { Name = name; LogFunc = logFunc }

    let asBytes log =
        let array = Array.zeroCreate<byte> <| 4 + log.Message.Length
        let span = Span array
        let status = span.Slice (0, 4)
        let msg = span.Slice (4, log.Message.Length)
        ((BitConverter.GetBytes (int log.Status)).AsSpan ()).CopyTo status
        ((log.Message |> Encoding.UTF8.GetBytes).AsSpan ()).CopyTo msg
        array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let status = BitConverter.ToInt32 (span.Slice (0, 4)) |> enum<Status>
        let msg = Encoding.UTF8.GetString (span.Slice (4, bytes.Length - 4))
        { Status = status; Message = msg }

    let log logger meta status format =
        let doAfter s =
            let m = MetaTrace.asBytes meta
            let v = MetaTrace.value meta
            let d = { Status = status; Message = s } |> asBytes
            logger.LogFunc logger.Name v.TraceId v.TypeName m d
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process meta format = log this meta Status.Processing format
        member this.Success meta format = log this meta Status.Successed format
        member this.Fail meta format = log this meta Status.Failed format