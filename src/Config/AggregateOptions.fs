namespace UniStream.Domain


[<Sealed>]
type AggregateOptions() =

    member val Capacity = 4096 with get, set

    member val Multiple = 3 with get, set

    member val Count = 64 with get, set
