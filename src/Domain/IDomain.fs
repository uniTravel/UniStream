namespace UniStream.Domain


type IAggregate =
    interface end

type IValue =
    interface end

type IWrapped<'v when 'v :> IValue> =
    abstract member Value : 'v

type IDomainEvent<'v, 'agg when 'v :> IValue and 'agg :> IAggregate> =
    abstract member Apply : ('agg -> 'agg * int * 'v )

type IDomainCommand<'v, 'agg, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg>> =
    abstract member Convert : 'e