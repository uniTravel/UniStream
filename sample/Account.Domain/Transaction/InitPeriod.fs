namespace Account.Domain

open System


type PeriodInited =
    { AccountId: Guid
      Period: string
      Limit: decimal }

    member me.Apply(agg: Transaction) =
        agg.AccountId <- me.AccountId
        agg.Period <- me.Period
        agg.Limit <- me.Limit
        agg.TransLimit <- me.Limit


type InitPeriod =
    { AccountId: Guid
      Period: string
      Limit: decimal }

    member me.Validate(agg: Transaction) =
        let period = $"{DateTime.Today:yyyyMM}"

        if me.Period <> period then
            raise <| ValidateError $"交易期间应为{period}，而实际传入为{me.Period}"

    member me.Execute(agg: Transaction) : PeriodInited =
        { AccountId = me.AccountId
          Period = me.Period
          Limit = me.Limit }
