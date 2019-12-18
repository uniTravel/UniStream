namespace UniStream.Domain

open System
open System.Text


module MetaEvent =

    type T =
        { Aggregate: Guid
          Trace: Guid
          Version: int
          Delta: string }

    let create (metaTrace: MetaTrace.T) version =
        { Aggregate = metaTrace.AggregateId; Trace = metaTrace.TraceId; Version = version; Delta = metaTrace.DeltaType }

    let asBytes metaEvent =
        let array = Array.zeroCreate<byte> <| 36 + metaEvent.Delta.Length
        let span = Span array
        let aId = span.Slice (0, 16)
        let tId = span.Slice (16, 16)
        let ver = span.Slice (32, 4)
        let name = span.Slice (36, metaEvent.Delta.Length)
        ((metaEvent.Aggregate.ToByteArray()).AsSpan()).CopyTo aId
        ((metaEvent.Trace.ToByteArray()).AsSpan()).CopyTo tId
        ((BitConverter.GetBytes metaEvent.Version).AsSpan()).CopyTo ver
        ((Encoding.UTF8.GetBytes metaEvent.Delta).AsSpan()).CopyTo name
        array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let aId = Guid (span.Slice (0, 16))
        let tId = Guid (span.Slice (16, 16))
        let ver = BitConverter.ToInt32 (span.Slice (32, 4))
        let name = Encoding.UTF8.GetString (span.Slice (36, bytes.Length - 36))
        { Aggregate = aId; Trace = tId; Version = ver; Delta = name }