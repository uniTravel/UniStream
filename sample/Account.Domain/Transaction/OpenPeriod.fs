namespace Account.Domain

open System
open System.ComponentModel.DataAnnotations


type PeriodOpened =
    { AccountId: Guid
      Period: string }

    member me.Apply(agg: Transaction) =
        agg.AccountId <- me.AccountId
        agg.Period <- me.Period


type OpenPeriod() =

    [<Required>]
    member val AccountId = Guid.Empty with get, set

    [<StringLength(6)>]
    member val Period = "" with get, set

    member me.Validate(agg: Transaction) =
        let next = DateTime.Today.AddMonths(1)
        let period = $"{next:yyyyMM}"

        if me.Period <> period then
            raise <| ValidateError $"交易期间应为{period}，而实际传入为{me.Period}"

    member me.Execute(agg: Transaction) : PeriodOpened =
        { AccountId = me.AccountId
          Period = me.Period }
