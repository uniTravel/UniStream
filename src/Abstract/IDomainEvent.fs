namespace UniStream.Abstract


type IDomainEvent<'v, 'agg when 'v :> IValue and 'agg :> IAggregate> =
    abstract member Apply : ('agg -> 'agg * int * 'v )