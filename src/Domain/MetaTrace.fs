namespace UniStream.Domain

open System
open System.Text


module MetaTrace =

    type T =
        { Aggregate: Guid
          Trace: Guid
          Delta: string
          Bytes: byte[] option }
        member this.AggregateId = this.Aggregate
        member this.TraceId = this.Trace
        member this.DeltaType = this.Delta

    let createImpl aggId deltaType =
        { Aggregate = aggId; Trace = Guid.NewGuid(); Delta = deltaType; Bytes = None }

    let inline create< ^d> (aggId: Guid) : T =
        createImpl aggId typeof< ^d>.FullName

    let asBytes metaTrace =
        match metaTrace.Bytes with
        | Some bytes -> bytes
        | None ->
            let array = Array.zeroCreate<byte> <| 32 + metaTrace.Delta.Length
            let span = Span array
            let aId = span.Slice (0, 16)
            let tId = span.Slice (16, 16)
            let name = span.Slice (32, metaTrace.Delta.Length)
            ((metaTrace.AggregateId.ToByteArray()).AsSpan()).CopyTo aId
            ((metaTrace.TraceId.ToByteArray()).AsSpan()).CopyTo tId
            ((Encoding.UTF8.GetBytes metaTrace.Delta).AsSpan()).CopyTo name
            array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let aId = Guid (span.Slice (0, 16))
        let tId = Guid (span.Slice (16, 16))
        let name = Encoding.UTF8.GetString (span.Slice (32, bytes.Length - 32))
        { Aggregate = aId; Trace = tId; Delta = name; Bytes = Some bytes }