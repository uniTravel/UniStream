namespace Account.Domain

open UniStream.Domain


[<Sealed>]
type Account(id) =
    inherit Aggregate(id)

    member val Code = "" with get, set

    member val Owner = "" with get, set

    member val Limit = 0m with get, set

    member val VerifiedBy = "" with get, set

    member val Verified = false with get, set

    member val VerifyConclusion = false with get, set

    member val ApprovedBy = "" with get, set

    member val Approved = false with get, set
