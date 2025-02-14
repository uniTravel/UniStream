namespace Account.Domain

open System.ComponentModel.DataAnnotations
open UniStream.Domain


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
            raise <| ValidateException $"已经审核，结论为：{conclusion}"

    member me.Execute(agg: Account) =
        { VerifiedBy = me.VerifiedBy
          Verified = true
          Conclusion = me.Conclusion }


type LimitAccount() =

    [<Required>]
    member val Limit = 0.0m with get, set

    member me.Validate(agg: Account) =
        if not agg.Approved then
            raise <| ValidateException "账户未批准"

        if me.Limit = agg.Limit then
            raise <| ValidateException "限额与原先一致"

        if me.Limit <= 0m then
            raise <| ValidateException "限额必须大于零"

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
            raise <| ValidateException "未审核通过"

        if me.Approved && me.Limit <= 0m then
            raise <| ValidateException "批准的账户，限额必须大于零"

    member me.Execute(agg: Account) =
        { AccountId = agg.Id
          ApprovedBy = me.ApprovedBy
          Approved = me.Approved
          Limit = if me.Approved then me.Limit else 0m }
