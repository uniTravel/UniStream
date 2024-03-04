namespace Account.Domain

open System


type AccountCreated =
    { Owner: string }

    member me.Apply(agg: Account) = agg.Owner <- me.Owner


type AccountVerified =
    { VerifiedBy: string
      Verified: bool
      Conclusion: bool }

    member me.Apply(agg: Account) =
        agg.VerifiedBy <- me.VerifiedBy
        agg.Verified <- me.Verified
        agg.VerifyConclusion <- me.Conclusion


type AccountLimited =
    { AccountId: Guid
      Limit: decimal }

    member me.Apply(agg: Account) = agg.Limit <- me.Limit


type AccountApproved =
    { AccountId: Guid
      ApprovedBy: string
      Approved: bool
      Limit: decimal
      CurrentPeriod: string
      NextPeriod: string }

    member me.Apply(agg: Account) =
        agg.ApprovedBy <- me.ApprovedBy
        agg.Approved <- me.Approved
        agg.Limit <- me.Limit
        agg.CurrentPeriod <- me.CurrentPeriod
        agg.NextPeriod <- me.NextPeriod


type PeriodChanged =
    { AccountId: Guid
      CurrentPeriod: string
      NextPeriod: string }

    member me.Apply(agg: Account) =
        agg.CurrentPeriod <- me.CurrentPeriod
        agg.NextPeriod <- me.NextPeriod
