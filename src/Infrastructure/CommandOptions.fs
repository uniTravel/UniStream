namespace UniStream.Domain


[<Sealed>]
type CommandOptions() =

    member val Interval = 10 with get,set