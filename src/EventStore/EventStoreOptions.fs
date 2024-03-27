namespace UniStream.Domain

open System


[<Sealed>]
type EventStoreOptions() =

    static member val Name = "EventStore"

    member val User = String.Empty with get, set

    member val Pass = String.Empty with get, set

    member val Host = String.Empty with get, set

    member val VerifyCert = true with get, set
