namespace Account.Domain

open System
open System.ComponentModel.DataAnnotations


type PeriodChanged =
    { AccountId: Guid
      CurrentPeriod: string
      NextPeriod: string }

    member me.Apply(agg: Account) =
        agg.CurrentPeriod <- me.CurrentPeriod
        agg.NextPeriod <- me.NextPeriod


type ChangePeriod() =

    [<DataType(DataType.Date)>]
    member val Today = DateTime.Today with get, set

    member me.Validate(agg: Account) =
        let current = $"{me.Today:yyyyMM}"

        if not agg.Approved then
            raise <| ValidateError "账户未批准"

        if agg.CurrentPeriod = current then
            raise <| ValidateError "相关交易期已处理"

        if agg.NextPeriod <> current then
            raise <| ValidateError $"当期应为{agg.NextPeriod}"

    member me.Execute(agg: Account) =
        let current = me.Today
        let next = current.AddMonths(1)

        { AccountId = agg.Id
          CurrentPeriod = $"{current:yyyyMM}"
          NextPeriod = $"{next:yyyyMM}" }
