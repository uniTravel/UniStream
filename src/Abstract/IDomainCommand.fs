namespace UniStream.Abstract


type IDomainCommand<'v, 'agg, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg>> =
    abstract member Convert : 'e