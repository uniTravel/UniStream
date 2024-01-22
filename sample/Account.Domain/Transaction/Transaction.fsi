namespace Account.Domain

open System
open UniStream.Domain


[<Sealed>]
type Transaction =
    inherit Aggregate

    new: Guid -> Transaction

    member AccountId: Guid with get
    member internal AccountId: Guid with set

    member Balance: decimal with get
    member internal Balance: decimal with set

    member Period: string with get
    member internal Period: string with set

    member Limit: decimal with get
    member internal Limit: decimal with set

    member TransLimit: decimal with get
    member internal TransLimit: decimal with set
