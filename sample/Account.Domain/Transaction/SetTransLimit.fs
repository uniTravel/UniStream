namespace Account.Domain

open System.ComponentModel.DataAnnotations


type TransLimitSetted =
    { TransLimit: decimal }

    member me.Apply(agg: Transaction) = agg.TransLimit <- me.TransLimit


type SetTransLimit() =

    [<Required>]
    member val TransLimit = 0m with get, set

    member me.Validate(agg: Transaction) =
        if me.TransLimit > agg.Limit then
            raise <| ValidateError "交易限额不得超过控制限额"

    member me.Execute(agg: Transaction) : TransLimitSetted = { TransLimit = me.TransLimit }
