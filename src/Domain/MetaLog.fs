namespace UniStream.Domain

open System

module MetaLog =

    type T = { AggregateId: Guid; TraceId: Guid }

    let value (metaLog: T) =
        {| AggregateId = metaLog.AggregateId; TraceId = metaLog.TraceId |}

    let create () =
        { AggregateId = Guid.NewGuid (); TraceId = Guid.NewGuid () }

    let asBytes (metaLog: T) : byte[] =
        failwith ""

    let fromBytes (bytes: byte[]) : T =
        failwith ""