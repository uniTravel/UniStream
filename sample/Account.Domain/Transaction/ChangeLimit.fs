namespace Account.Domain

open System.ComponentModel.DataAnnotations


type LimitChanged =
    { Limit: decimal
      TransLimit: decimal }

    member me.Apply(agg: Transaction) =
        agg.Limit <- me.Limit
        agg.TransLimit <- me.TransLimit


type ChangeLimit() =

    [<Required>]
    member val Limit = 0m with get, set

    member me.Validate(agg: Transaction) = ()

    member me.Execute(agg: Transaction) =
        { Limit = me.Limit
          TransLimit =
            if agg.TransLimit > me.Limit then
                me.Limit
            else
                agg.TransLimit }
