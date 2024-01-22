namespace Account.Domain

open System


type PeriodOpened =
    { AccountId: Guid
      Period: string }

    member me.Apply(agg: Transaction) =
        agg.AccountId <- me.AccountId
        agg.Period <- me.Period


type OpenPeriod =
    { AccountId: Guid
      Period: string }

    member me.Validate(agg: Transaction) =
        let next = DateTime.Today.AddMonths(1)
        let period = $"{next:yyyyMM}"

        if me.Period <> period then
            raise <| ValidateError $"交易期间应为{period}，而实际传入为{me.Period}"

    member me.Execute(agg: Transaction) : PeriodOpened =
        { AccountId = me.AccountId
          Period = me.Period }
