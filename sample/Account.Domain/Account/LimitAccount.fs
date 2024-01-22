namespace Account.Domain

open System


type AccountLimited =
    { AccountId: Guid
      Limit: decimal }

    member me.Apply(agg: Account) = agg.Limit <- me.Limit


type LimitAccount =
    { Limit: decimal }

    member me.Validate(agg: Account) =
        if not agg.Approved then
            raise <| ValidateError "账户未批准"

        if me.Limit = agg.Limit then
            raise <| ValidateError "限额与原先一致"

        if me.Limit <= 0m then
            raise <| ValidateError "限额必须大于零"

    member me.Execute(agg: Account) =
        { AccountId = agg.Id; Limit = me.Limit }
