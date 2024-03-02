namespace Account.Domain

open System.ComponentModel.DataAnnotations


type AccountCreated =
    { Owner: string }

    member me.Apply(agg: Account) = agg.Owner <- me.Owner


type CreateAccount() =

    [<Required>]
    member val Owner = "" with get, set

    member me.Validate(agg: Account) = ()

    member me.Execute(agg: Account) : AccountCreated = { Owner = me.Owner }
