namespace UniStream.Domain

open System
open System.IO

module MetaEvent =

    type T = { AggregateId: Guid; TraceId: Guid; Version: int }

    let value (metaEvent: T) =
        {| AggregateId = metaEvent.AggregateId; TraceId = metaEvent.TraceId; Version = metaEvent.Version |}

    let create (metaLog: MetaLog.T) version : T =
        let log = MetaLog.value metaLog
        { AggregateId = log.AggregateId; TraceId = log.TraceId; Version = version }

    let asBytes (metaEvent: T) : byte[] =
        let array = Array.zeroCreate 36
        use memStream = new MemoryStream (array)
        use writer = new BinaryWriter (memStream)
        writer.Write (metaEvent.AggregateId.ToByteArray ())
        writer.Write (metaEvent.TraceId.ToByteArray ())
        array

    let fromBytes (bytes: byte[]) : T =
        let reader = ReadOnlySpan bytes
        let aggregateId = Guid (reader.Slice (0, 16))
        let traceId = Guid (reader.Slice (16,16))
        // let version =
        failwith ""

    // let create index chunkSize =
    //     { _chunkNumber = index; _chunkSize = chunkSize; _chunkId = Guid.NewGuid () }

    // let fromStream (OriginStream stream) =
    //     let reader = new BinaryReader(stream)
    //     stream.Seek (0L, SeekOrigin.Begin) |> ignore
    //     let chunkNumber = reader.ReadInt32 ()
    //     let chunkSize = reader.ReadInt64 ()
    //     let chunkId = Guid (reader.ReadBytes (16))
    //     { _chunkNumber = chunkNumber; _chunkSize = chunkSize; _chunkId = chunkId }

    // let asByteArray (chunkHeader: ChunkHeader) =
    //     let array = Array.zeroCreate <| int size
    //     use memStream = new MemoryStream (array)
    //     use writer = new BinaryWriter (memStream)
    //     writer.Write (chunkHeader._chunkNumber)
    //     writer.Write (chunkHeader._chunkSize)
    //     writer.Write (chunkHeader._chunkId.ToByteArray ())
    //     array
