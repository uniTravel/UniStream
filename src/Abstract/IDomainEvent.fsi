namespace UniStream.Abstract


/// <summary>领域事件接口
/// </summary>
/// <typeparam name="'v">领域值类型。</typeparam>
/// <typeparam name="'agg">聚合类型。</typeparam>
[<Interface>]
type IDomainEvent<'v, 'agg when 'v :> IValue and 'agg :> IAggregate> =

    /// <summary>应用事件
    /// </summary>
    /// <param name="agg">应用事件的聚合。</param>
    /// <returns>应用事件之后的聚合 * 事件版本 * 领域事件值。</returns>
    abstract member Apply : ('agg -> 'agg * int * 'v)