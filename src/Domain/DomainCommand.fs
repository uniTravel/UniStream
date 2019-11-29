namespace UniStream.Domain


module DomainCommand =

    let create<'v, 'agg, 'c, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg> and 'c :> IDomainCommand<'v, 'agg, 'e>> isValid (ctor: 'v -> 'c) c =
        if isValid c
        then ctor c
        else failwithf "值验证错误：%A" c

    let apply<'v, 'a when 'v :> IValue> (f: 'v -> 'a) (c: IWrapped<'v>) =
        c.Value |> f

    let value c =
        apply id c

    let equals left right =
        (value left) = (value right)