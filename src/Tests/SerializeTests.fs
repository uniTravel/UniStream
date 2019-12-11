module Serialize.Tests

open System
open System.IO
open System.Text.Json
open Expecto

[<CLIMutable>]
type MetaEvent = { AggregateId: Guid; TraceId: Guid; Version: int }

[<PTests>]
let testBinarySerialize =
    testSequenced <| testList "二进制序列化" [
        let withArgs f () =
            let e = { AggregateId = Guid.NewGuid (); TraceId = Guid.NewGuid (); Version = 7 }
            let d (array: byte[]) =
                let a = Guid array.[0 .. 15]
                let t = Guid array.[16 .. 31]
                let v = BitConverter.ToInt32 (ReadOnlySpan array.[32 .. 35])
                { AggregateId = a; TraceId = t; Version = v }
            go "二进制序列化" |> f e d
        yield! testFixture withArgs [
            "MemoryStream", fun e d finish ->
                let array = Array.zeroCreate 36
                use memStream = new MemoryStream (array)
                use writer = new BinaryWriter (memStream)
                writer.Write (e.AggregateId.ToByteArray ())
                writer.Write (e.TraceId.ToByteArray ())
                writer.Write e.Version
                let de = d array
                Expect.equal de.AggregateId e.AggregateId "AggregateId不相等"
                Expect.equal de.TraceId e.TraceId "TraceId不相等"
                Expect.equal de.Version e.Version "Version不相等"
                finish "MemoryStream"
            "Span", fun e d finish ->
                let array = Array.zeroCreate<byte> 36
                let span = Span array
                let a = span.Slice (0, 16)
                let t = span.Slice (16, 16)
                let v = span.Slice (32, 4)
                ((e.AggregateId.ToByteArray ()).AsSpan ()).CopyTo a
                ((e.TraceId.ToByteArray ()).AsSpan ()).CopyTo t
                ((BitConverter.GetBytes e.Version).AsSpan ()).CopyTo v
                let de = d array
                Expect.equal de.AggregateId e.AggregateId "AggregateId不相等"
                Expect.equal de.TraceId e.TraceId "TraceId不相等"
                Expect.equal de.Version e.Version "Version不相等"
                finish "Span"
            "数组", fun e d finish ->
                let a = e.AggregateId.ToByteArray ()
                let t = e.TraceId.ToByteArray ()
                let v = BitConverter.GetBytes e.Version
                let array = Array.concat [ a; t; v]
                let de = d array
                Expect.equal de.AggregateId e.AggregateId "AggregateId不相等"
                Expect.equal de.TraceId e.TraceId "TraceId不相等"
                Expect.equal de.Version e.Version "Version不相等"
                finish "数组"
        ]
    ]
    |> testLabel "UniStream.Domain"

[<PTests>]
let testJsonSerialize =
    testSequenced <| testList "Json序列化" [
        let withArgs f () =
            let e = { AggregateId = Guid.NewGuid (); TraceId = Guid.NewGuid (); Version = 7 }
            go "Json序列化" |> f e
        yield! testFixture withArgs [
            "Utf8", fun e finish ->
                let array = JsonSerializer.SerializeToUtf8Bytes e
                let span = ReadOnlySpan array
                let de = JsonSerializer.Deserialize<MetaEvent> span
                Expect.equal de.AggregateId e.AggregateId "AggregateId不相等"
                Expect.equal de.TraceId e.TraceId "TraceId不相等"
                Expect.equal de.Version e.Version "Version不相等"
                finish "Utf8"
        ]
    ]
    |> testLabel "UniStream.Domain"

[<PTests>]
let testBinaryDeserialize =
    testSequenced <| testList "二进制反序列化" [
        let withArgs f () =
            let e = { AggregateId = Guid.NewGuid (); TraceId = Guid.NewGuid (); Version = 7 }
            let array = Array.zeroCreate<byte> 36
            let span = Span array
            let a = span.Slice (0, 16)
            let t = span.Slice (16, 16)
            let v = span.Slice (32, 4)
            ((e.AggregateId.ToByteArray ()).AsSpan ()).CopyTo a
            ((e.TraceId.ToByteArray ()).AsSpan ()).CopyTo t
            ((BitConverter.GetBytes e.Version).AsSpan ()).CopyTo v
            go "二进制反序列化" |> f e array
        yield! testFixture withArgs [
            "MemoryStream", fun e array finish ->
                use memStream = new MemoryStream (array)
                use reader = new BinaryReader (memStream)
                let a = Guid (reader.ReadBytes 16)
                let t = Guid (reader.ReadBytes 16)
                let v = reader.ReadInt32 ()
                let de = { AggregateId = a; TraceId = t; Version = v }
                Expect.equal de.AggregateId e.AggregateId "AggregateId不相等"
                Expect.equal de.TraceId e.TraceId "TraceId不相等"
                Expect.equal de.Version e.Version "Version不相等"
                finish "MemoryStream"
            "Span", fun e array finish ->
                let span = ReadOnlySpan array
                let a = Guid (span.Slice (0, 16))
                let t = Guid (span.Slice (16, 16))
                let v = BitConverter.ToInt32 (span.Slice (32, 4))
                let de = { AggregateId = a; TraceId = t; Version = v }
                Expect.equal de.AggregateId e.AggregateId "AggregateId不相等"
                Expect.equal de.TraceId e.TraceId "TraceId不相等"
                Expect.equal de.Version e.Version "Version不相等"
                finish "Span"
            "数组", fun e array finish ->
                let a = Guid array.[0 .. 15]
                let t = Guid array.[16 .. 31]
                let v = BitConverter.ToInt32 (ReadOnlySpan array.[32 .. 35])
                let de = { AggregateId = a; TraceId = t; Version = v }
                Expect.equal de.AggregateId e.AggregateId "AggregateId不相等"
                Expect.equal de.TraceId e.TraceId "TraceId不相等"
                Expect.equal de.Version e.Version "Version不相等"
                finish "数组"
        ]
    ]
    |> testLabel "UniStream.Domain"