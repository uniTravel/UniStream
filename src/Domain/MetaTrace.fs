namespace UniStream.Domain

open System
open System.Text


module MetaTrace =

    type T = { AggregateId: Guid; TraceId: Guid; TypeName: string }

    let value meta =
        {| AggregateId = meta.AggregateId; TraceId = meta.TraceId; TypeName = meta.TypeName |}

    let create<'v when 'v :> IValue> aggId =
        { AggregateId = aggId; TraceId = Guid.NewGuid (); TypeName = typeof<'v>.FullName }

    let asBytes meta =
        let array = Array.zeroCreate<byte> <| 32 + meta.TypeName.Length
        let span = Span array
        let aId = span.Slice (0, 16)
        let tId = span.Slice (16, 16)
        let name = span.Slice (32, meta.TypeName.Length)
        ((meta.AggregateId.ToByteArray ()).AsSpan ()).CopyTo aId
        ((meta.TraceId.ToByteArray ()).AsSpan ()).CopyTo tId
        ((meta.TypeName |> Encoding.UTF8.GetBytes).AsSpan ()).CopyTo name
        array

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        let aId = Guid (span.Slice (0, 16))
        let tId = Guid (span.Slice (16, 16))
        let name = Encoding.UTF8.GetString (span.Slice (32, bytes.Length - 32))
        { AggregateId = aId; TraceId = tId; TypeName = name }