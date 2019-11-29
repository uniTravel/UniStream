namespace UniStream.Domain

open UniStream.Abstract


module DomainEvent =

    let create<'v, 'agg, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg>> (ctor: 'v -> 'e) e =
        ctor e

    let apply<'v, 'a when 'v :> IValue> (f: 'v -> 'a) (e: IWrapped<'v>) =
        e.Value |> f

    let value e =
        apply id e

    let equals left right =
        (value left) = (value right)