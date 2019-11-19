module Deserialize

open System
open System.IO
open BenchmarkDotNet.Attributes

type MetaEvent = { AggregateId: Guid; TraceId: Guid; Version: int }

[<MemoryDiagnoser>]
type Deserialize () =

    let e = { AggregateId = Guid.NewGuid (); TraceId = Guid.NewGuid (); Version = 1 }
    let array = Array.zeroCreate<byte> 36

    [<Params(1000000)>]
    member val public count = 0 with get, set

    [<GlobalSetup>]
    member _.GlobalSetup () =
        let span = Span array
        let a = span.Slice (0, 16)
        let t = span.Slice (16, 16)
        let v = span.Slice (32, 4)
        ((e.AggregateId.ToByteArray ()).AsSpan ()).CopyTo a
        ((e.TraceId.ToByteArray ()).AsSpan ()).CopyTo t
        ((BitConverter.GetBytes e.Version).AsSpan ()).CopyTo v

    [<Benchmark>]
    member self.MemoryStream () =
        [ 1 .. self.count ]
        |> List.iter (fun i ->
            use memStream = new MemoryStream (array)
            use reader = new BinaryReader (memStream)
            let a = Guid (reader.ReadBytes 16)
            let t = Guid (reader.ReadBytes 16)
            let v = reader.ReadInt32 ()
            let de = { AggregateId = a; TraceId = t; Version = v }
            ()
        )

    [<Benchmark>]
    member self.Span () =
        [ 1 .. self.count ]
        |> List.iter (fun i ->
            let span = ReadOnlySpan array
            let a = Guid (span.Slice (0, 16))
            let t = Guid ((span.Slice (16, 16)).ToArray ())
            let v = BitConverter.ToInt32 (span.Slice (32, 4))
            let de = { AggregateId = a; TraceId = t; Version = v }
            ()
        )

    [<Benchmark>]
    member self.Array () =
        [ 1 .. self.count ]
        |> List.iter (fun i ->
            let a = Guid array.[0 .. 15]
            let t = Guid array.[16 .. 31]
            let v = BitConverter.ToInt32 (ReadOnlySpan array.[32 .. 35])
            let de = { AggregateId = a; TraceId = t; Version = v }
            ()
        )