namespace Account.Domain


type LimitChanged =
    { Limit: decimal
      TransLimit: decimal }

    member me.Apply(agg: Transaction) =
        agg.Limit <- me.Limit
        agg.TransLimit <- me.TransLimit


type ChangeLimit =
    { Limit: decimal }

    member me.Validate(agg: Transaction) = ()

    member me.Execute(agg: Transaction) =
        { Limit = me.Limit
          TransLimit =
            if agg.TransLimit > me.Limit then
                me.Limit
            else
                agg.TransLimit }
