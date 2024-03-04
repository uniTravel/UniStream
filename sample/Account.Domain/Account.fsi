namespace Account.Domain

open System
open UniStream.Domain


[<Sealed>]
type Account =
    inherit Aggregate

    new: id: Guid -> Account

    member Code: string with get
    member internal Code: string with set

    member Owner: string with get
    member internal Owner: string with set

    member Limit: decimal with get
    member internal Limit: decimal with set

    member Verified: bool with get
    member internal Verified: bool with set

    member VerifiedBy: string with get
    member internal VerifiedBy: string with set

    member VerifyConclusion: bool with get
    member internal VerifyConclusion: bool with set

    member Approved: bool with get
    member internal Approved: bool with set

    member ApprovedBy: string with get
    member internal ApprovedBy: string with set

    member CurrentPeriod: string with get
    member internal CurrentPeriod: string with set

    member NextPeriod: string with get
    member internal NextPeriod: string with set
