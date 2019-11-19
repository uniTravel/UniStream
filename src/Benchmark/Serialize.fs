module Serialize

open System
open System.IO
open BenchmarkDotNet.Attributes

type MetaEvent = { AggregateId: Guid; TraceId: Guid; Version: int }

[<MemoryDiagnoser>]
type Serialize () =

    let aggId = Guid.NewGuid()
    let traceId = Guid.NewGuid()

    [<Params(1000000)>]
    member val public count = 0 with get, set

    [<Benchmark>]
    member self.MemoryStream () =
        [ 1 .. self.count ]
        |> List.iter (fun i ->
            let e = { AggregateId = aggId; TraceId = traceId; Version = i }
            let array = Array.zeroCreate 36
            use memStream = new MemoryStream (array)
            use writer = new BinaryWriter (memStream)
            writer.Write (e.AggregateId.ToByteArray ())
            writer.Write (e.TraceId.ToByteArray ())
            writer.Write e.Version
        )

    [<Benchmark>]
    member self.Span () =
        [ 1 .. self.count ]
        |> List.iter (fun i ->
            let e = { AggregateId = aggId; TraceId = traceId; Version = i }
            let array = Array.zeroCreate<byte> 36
            let span = Span array
            let a = span.Slice (0, 16)
            let t = span.Slice (16, 16)
            let v = span.Slice (32, 4)
            ((e.AggregateId.ToByteArray ()).AsSpan ()).CopyTo a
            ((e.TraceId.ToByteArray ()).AsSpan ()).CopyTo t
            ((BitConverter.GetBytes e.Version).AsSpan ()).CopyTo v
        )

    [<Benchmark>]
    member self.Array () =
        [ 1 .. self.count ]
        |> List.iter (fun i ->
            let e = { AggregateId = aggId; TraceId = traceId; Version = i }
            let a = e.AggregateId.ToByteArray ()
            let t = e.TraceId.ToByteArray ()
            let v = BitConverter.GetBytes e.Version
            let array = Array.concat [ a; t; v]
            ()
        )