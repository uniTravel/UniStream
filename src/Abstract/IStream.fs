namespace UniStream.Domain

open System


type IStream =

    abstract member Writer: (Guid option -> string -> Guid -> uint64 -> string -> byte array -> unit)

    abstract member Reader: (string -> Guid -> (string * byte array) list)
