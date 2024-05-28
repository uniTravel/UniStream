namespace UniStream.Domain

open System


type IStream =

    abstract member Writer: (string -> Guid -> uint64 -> string -> byte array -> unit)

    abstract member Reader: (string -> Guid -> (string * ReadOnlyMemory<byte>) list)
