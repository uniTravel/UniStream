namespace Account.Domain

open System


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


type ApproveAccount =
    { ApprovedBy: string
      Approved: bool
      Limit: decimal }

    member me.Validate(agg: Account) =
        if not agg.VerifyConclusion then
            raise <| ValidateError "未审核通过"

        if me.Approved && me.Limit <= 0m then
            raise <| ValidateError "批准的账户，限额必须大于零"

    member me.Execute(agg: Account) =
        let current = DateTime.Today
        let next = current.AddMonths(1)

        if me.Approved then
            let current = DateTime.Today
            let next = current.AddMonths(1)
            agg.CurrentPeriod <- $"{current:yyyyMM}"
            agg.NextPeriod <- $"{next:yyyyMM}"

        { AccountId = agg.Id
          ApprovedBy = me.ApprovedBy
          Approved = me.Approved
          Limit = if me.Approved then me.Limit else 0m
          CurrentPeriod = if me.Approved then $"{current:yyyyMM}" else ""
          NextPeriod = if me.Approved then $"{next:yyyyMM}" else "" }
