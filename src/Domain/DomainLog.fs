namespace UniStream.Domain

open System
open System.Text


module DomainLog =

    type Status =
        | Processing = 0
        | Successed = 1
        | Failed = 2

    type T = { _status: Status; _message: string }

    type Logger = { _name: string; _logFunc: string -> Guid -> string -> byte[] -> byte[] -> unit }

    let logger aggType logFunc =
        { _name = aggType; _logFunc = logFunc }

    let asBytes log =
        let array = Array.zeroCreate<byte> <| 4 + log._message.Length
        let span = Span array
        let status = span.Slice (0, 4)
        let msg = span.Slice (4, log._message.Length)
        ((BitConverter.GetBytes (int log._status)).AsSpan()).CopyTo status
        ((Encoding.UTF8.GetBytes log._message).AsSpan()).CopyTo msg
        array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let status = BitConverter.ToInt32 (span.Slice (0, 4)) |> enum<Status>
        let msg = Encoding.UTF8.GetString (span.Slice (4, bytes.Length - 4))
        { _status = status; _message = msg }

    let log logger metaTrace status format =
        let doAfter s =
            let m = MetaTrace.asBytes metaTrace
            let d = { _status = status; _message = s } |> asBytes
            logger._logFunc logger._name metaTrace.TraceId metaTrace.DeltaType m d
        Printf.ksprintf doAfter format

    type Logger with
        member this.Process metaTrace format = log this metaTrace Status.Processing format
        member this.Success metaTrace format = log this metaTrace Status.Successed format
        member this.Fail metaTrace format = log this metaTrace Status.Failed format