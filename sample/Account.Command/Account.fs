namespace Account.Domain

open System.ComponentModel.DataAnnotations
open UniStream.Domain


type CreateAccount() =

    [<Required(ErrorMessage = ValidateError.owner)>]
    member val Owner = "" with get, set

    member me.Validate(agg: Account) = ()

    member me.Execute(agg: Account) = { Owner = me.Owner }


type VerifyAccount() =

    [<Required(ErrorMessage = ValidateError.verifiedBy)>]
    member val VerifiedBy = "" with get, set

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

    [<Range(typeof<decimal>, "100", "100000", ErrorMessage = ValidateError.limit)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Limit = 0.0m with get, set

    member me.Validate(agg: Account) =
        if not agg.Approved then
            raise <| ValidateException "账户未批准"

        if me.Limit = agg.Limit then
            raise <| ValidateException "限额与原先一致"

    member me.Execute(agg: Account) =
        { AccountId = agg.Id; Limit = me.Limit }


type ApproveAccount() =

    [<Required(ErrorMessage = ValidateError.approvedBy)>]
    member val ApprovedBy = "" with get, set

    member val Approved = false with get, set

    [<Range(typeof<decimal>, "100", "100000", ErrorMessage = ValidateError.limit)>]
    [<RegularExpression(@"^\d+(\.\d{1,2})?$", ErrorMessage = ValidateError.money)>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Account) =
        if not agg.VerifyConclusion then
            raise <| ValidateException "未审核通过"

    member me.Execute(agg: Account) =
        { AccountId = agg.Id
          ApprovedBy = me.ApprovedBy
          Approved = me.Approved
          Limit = if me.Approved then me.Limit else 0m }

    interface IValidatableObject with
        member me.Validate(validationContext: ValidationContext) =
            [ if me.Approved && me.Limit <= 0m then
                  yield ValidationResult ValidateError.approvedLimit ]
