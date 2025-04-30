namespace UniStream.Domain

open System


[<Sealed>]
type KurrentOptions() =

    static member val Name = "Kurrent"

    member val User = String.Empty with get, set

    member val Pass = String.Empty with get, set

    member val Host = String.Empty with get, set

    member val VerifyCert = true with get, set
