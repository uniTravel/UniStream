namespace UniStream.Domain

open System
open System.Text


module MetaEvent =

    type T = {
        _aggregateId: Guid
        _traceId: Guid
        _version: int
        _deltaType: string
    }

    let create (metaTrace: MetaTrace.T) version =
        { _aggregateId = metaTrace.AggregateId; _traceId = metaTrace.TraceId; _version = version; _deltaType = metaTrace.DeltaType }

    let asBytes metaEvent =
        let array = Array.zeroCreate<byte> <| 36 + metaEvent._deltaType.Length
        let span = Span array
        let aId = span.Slice (0, 16)
        let tId = span.Slice (16, 16)
        let ver = span.Slice (32, 4)
        let name = span.Slice (36, metaEvent._deltaType.Length)
        ((metaEvent._aggregateId.ToByteArray()).AsSpan()).CopyTo aId
        ((metaEvent._traceId.ToByteArray()).AsSpan()).CopyTo tId
        ((BitConverter.GetBytes metaEvent._version).AsSpan()).CopyTo ver
        ((Encoding.UTF8.GetBytes metaEvent._deltaType).AsSpan()).CopyTo name
        array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let aId = Guid (span.Slice (0, 16))
        let tId = Guid (span.Slice (16, 16))
        let ver = BitConverter.ToInt32 (span.Slice (32, 4))
        let name = Encoding.UTF8.GetString (span.Slice (36, bytes.Length - 36))
        { _aggregateId = aId; _traceId = tId; _version = ver; _deltaType = name }