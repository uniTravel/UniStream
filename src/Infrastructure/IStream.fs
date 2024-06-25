namespace UniStream.Domain

open System
open System.Collections.Generic


type IStream<'agg when 'agg :> Aggregate> =

    abstract member Writer: (Guid -> Guid -> uint64 -> string -> byte array -> unit)

    abstract member Reader: (Guid -> (string * ReadOnlyMemory<byte>) list)

    abstract member Restore: (HashSet<Guid> -> int -> Guid list)
