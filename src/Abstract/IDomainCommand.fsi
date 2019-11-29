namespace UniStream.Abstract


/// <summary>领域命令接口
/// </summary>
/// <typeparam name="'v">领域值类型。</typeparam>
/// <typeparam name="'agg">聚合类型。</typeparam>
/// <typeparam name="'e">领域事件类型。</typeparam>
[<Interface>]
type IDomainCommand<'v, 'agg, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg>> =

    /// <summary>命令对应的事件
    /// </summary>
    abstract member Convert : 'e