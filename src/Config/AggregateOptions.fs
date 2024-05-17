namespace UniStream.Domain


[<Sealed>]
type AggregateOptions() =

    member val Capacity = 10000 with get, set

    member val Multiple = 3 with get, set
