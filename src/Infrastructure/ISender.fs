namespace UniStream.Domain

open System


type ISender<'agg when 'agg :> Aggregate> =

    abstract member send: (Guid -> Guid -> string -> byte array -> Async<Result<unit, exn>>)
