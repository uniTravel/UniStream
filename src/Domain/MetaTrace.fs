namespace UniStream.Domain

open System
open System.Text


module MetaTrace =

    type T = {
        _aggregateId: Guid
        _traceId: Guid
        _deltaType: string
        _bytes: byte[] option
    } with
        member this.AggregateId = this._aggregateId
        member this.TraceId = this._traceId
        member this.DeltaType = this._deltaType

    let createImpl aggId deltaType =
        { _aggregateId = aggId; _traceId = Guid.NewGuid (); _deltaType = deltaType; _bytes = None }

    let inline create< ^d when ^d : (static member DeltaType: string)> (aggId: Guid) : T =
        createImpl aggId (^d : (static member DeltaType: string) ())

    let asBytes metaTrace =
        match metaTrace._bytes with
        | Some bytes -> bytes
        | None ->
            let array = Array.zeroCreate<byte> <| 32 + metaTrace._deltaType.Length
            let span = Span array
            let aId = span.Slice (0, 16)
            let tId = span.Slice (16, 16)
            let name = span.Slice (32, metaTrace._deltaType.Length)
            ((metaTrace._aggregateId.ToByteArray ()).AsSpan ()).CopyTo aId
            ((metaTrace._traceId.ToByteArray ()).AsSpan ()).CopyTo tId
            ((metaTrace._deltaType |> Encoding.UTF8.GetBytes).AsSpan ()).CopyTo name
            array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let aId = Guid (span.Slice (0, 16))
        let tId = Guid (span.Slice (16, 16))
        let name = Encoding.UTF8.GetString (span.Slice (32, bytes.Length - 32))
        { _aggregateId = aId; _traceId = tId; _deltaType = name; _bytes = Some bytes }