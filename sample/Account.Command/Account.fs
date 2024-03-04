namespace Account.Domain

open System
open System.ComponentModel.DataAnnotations


type CreateAccount() =

    [<Required>]
    member val Owner = "" with get, set

    member me.Validate(agg: Account) = ()

    member me.Execute(agg: Account) = { Owner = me.Owner }


type VerifyAccount() =

    [<Required>]
    member val VerifiedBy = "" with get, set

    [<Required>]
    member val Conclusion = false with get, set

    member me.Validate(agg: Account) =
        if agg.Verified then
            let conclusion = if agg.VerifyConclusion then "审核通过" else "审核未通过"
            raise <| ValidateError $"已经审核，结论为：{conclusion}"

    member me.Execute(agg: Account) =
        { VerifiedBy = me.VerifiedBy
          Verified = true
          Conclusion = me.Conclusion }


type LimitAccount() =

    [<Required>]
    member val Limit = 0.0m with get, set

    member me.Validate(agg: Account) =
        if not agg.Approved then
            raise <| ValidateError "账户未批准"

        if me.Limit = agg.Limit then
            raise <| ValidateError "限额与原先一致"

        if me.Limit <= 0m then
            raise <| ValidateError "限额必须大于零"

    member me.Execute(agg: Account) =
        { AccountId = agg.Id; Limit = me.Limit }


type ApproveAccount() =

    [<Required>]
    member val ApprovedBy = "" with get, set

    [<Required>]
    member val Approved = false with get, set

    [<Required>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Account) =
        if not agg.VerifyConclusion then
            raise <| ValidateError "未审核通过"

        if me.Approved && me.Limit <= 0m then
            raise <| ValidateError "批准的账户，限额必须大于零"

    member me.Execute(agg: Account) =
        let current = DateTime.Today
        let next = current.AddMonths(1)

        { AccountId = agg.Id
          ApprovedBy = me.ApprovedBy
          Approved = me.Approved
          Limit = if me.Approved then me.Limit else 0m
          CurrentPeriod = if me.Approved then $"{current:yyyyMM}" else ""
          NextPeriod = if me.Approved then $"{next:yyyyMM}" else "" }


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
