namespace UniStream.Domain

type IAggregate =
    interface end

type IValue =
    interface end

type IWrapped<'v when 'v :> IValue> =
    abstract Value : 'v

type IDomainEvent<'agg when 'agg :> IAggregate> =
    abstract member Apply : ('agg -> 'agg)

type IDomainCommand<'agg, 'e when 'agg :> IAggregate and 'e :> IDomainEvent<'agg>> =
    abstract member Convert : 'e