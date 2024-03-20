namespace UniStream.Infrastructure

open System


[<Sealed>]
type Options() =

    static member val Stream = "Stream"

    member val User = "admin" with get, set

    member val Pass = String.Empty with get, set

    member val Host = String.Empty with get, set

    member val VerifyCert = true with get, set

    member val Capacity = 10000 with get, set

    member val Refresh = 0.2 with get, set
