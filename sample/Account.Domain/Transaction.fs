namespace Account.Domain

open System
open UniStream.Domain


[<Sealed>]
type Transaction(id) =
    inherit Aggregate(id)

    member val AccountId = Guid.Empty with get, set

    member val Balance = 0m with get, set

    member val Period = "000001" with get, set

    member val Limit = 0m with get, set

    member val TransLimit = 0m with get, set
