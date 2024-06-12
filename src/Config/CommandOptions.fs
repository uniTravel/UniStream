namespace UniStream.Domain


[<Sealed>]
type CommandOptions() =

    member val Interval = 15 with get,set