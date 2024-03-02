namespace Account.Domain

open System.ComponentModel.DataAnnotations


type LimitSetted =
    { Limit: decimal
      TransLimit: decimal
      Balance: decimal }

    member me.Apply(agg: Transaction) =
        agg.Limit <- me.Limit
        agg.TransLimit <- me.TransLimit
        agg.Balance <- me.Balance


type SetLimit() =

    [<Required>]
    member val Limit = 0m with get, set

    [<Required>]
    member val TransLimit = 0m with get, set

    [<Required>]
    member val Balance = 0m with get, set

    member me.Validate(agg: Transaction) =
        if agg.Limit <> 0m then
            raise <| ValidateError "不得重复设置限额"

    member me.Execute(agg: Transaction) : LimitSetted =
        { Limit = me.Limit
          TransLimit = me.TransLimit
          Balance = me.Balance }
