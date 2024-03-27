namespace UniStream.Domain


[<Sealed>]
type AggregateOptions() =

    member val Capacity = 0 with get, set

    member val Refresh = 0.0 with get, set
