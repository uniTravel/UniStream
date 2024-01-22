namespace Account.Domain


type AccountCreated =
    { Owner: string }

    member me.Apply(agg: Account) = agg.Owner <- me.Owner


type CreateAccount =
    { Owner: string }

    member me.Validate(agg: Account) = ()

    member me.Execute(agg: Account) : AccountCreated = { Owner = me.Owner }
