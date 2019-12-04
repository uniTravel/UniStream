namespace UniStream.Domain

open System
open System.Text


module MetaTrace =

    type T = {
        _aggregateId: Guid
        _traceId: Guid
        _typeName: string
        _bytes: byte[] option
    } with
        member this.AggregateId = this._aggregateId
        member this.TraceId = this._traceId
        member this.TypeName = this._typeName

    let create<'v when 'v :> IValue> aggId =
        { _aggregateId = aggId; _traceId = Guid.NewGuid (); _typeName = typeof<'v>.FullName; _bytes = None }

    let asBytes meta =
        match meta._bytes with
        | Some bytes -> bytes
        | None ->
            let array = Array.zeroCreate<byte> <| 32 + meta._typeName.Length
            let span = Span array
            let aId = span.Slice (0, 16)
            let tId = span.Slice (16, 16)
            let name = span.Slice (32, meta._typeName.Length)
            ((meta._aggregateId.ToByteArray ()).AsSpan ()).CopyTo aId
            ((meta._traceId.ToByteArray ()).AsSpan ()).CopyTo tId
            ((meta._typeName |> Encoding.UTF8.GetBytes).AsSpan ()).CopyTo name
            array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let aId = Guid (span.Slice (0, 16))
        let tId = Guid (span.Slice (16, 16))
        let name = Encoding.UTF8.GetString (span.Slice (32, bytes.Length - 32))
        { _aggregateId = aId; _traceId = tId; _typeName = name; _bytes = Some bytes }