namespace UniStream.Domain


/// <summary>聚合接口
/// <para>聚合需要实现该接口。</para>
/// </summary>
[<Interface>]
type IAggregate =
    interface end


/// <summary>领域值接口
/// <para>领域命令、领域事件的值类型需要实现该接口。</para>
/// </summary>
[<Interface>]
type IValue =
    interface end


/// <summary>领域值包装接口
/// <para>领域命令、领域事件需要实现该接口。</para>
/// </summary>
/// <typeparam name="v">领域值类型。</typeparam>
[<Interface>]
type IWrapped<'v when 'v :> IValue> =

    /// <summary>领域值
    /// </summary>
    abstract Value : 'v


/// <summary>领域事件接口
/// </summary>
/// <typeparam name="agg">聚合类型。</typeparam>
[<Interface>]
type IDomainEvent<'agg when 'agg :> IAggregate> =

    /// <summary>应用事件
    /// </summary>
    /// <param name="agg">应用事件的聚合。</param>
    /// <returns>应用事件之后的聚合。</returns>
    abstract member Apply : ('agg -> 'agg)


/// <summary>领域命令接口
/// </summary>
/// <typeparam name="agg">聚合类型。</typeparam>
/// <typeparam name="e">领域事件类型。</typeparam>
[<Interface>]
type IDomainCommand<'agg, 'e when 'agg :> IAggregate and 'e :> IDomainEvent<'agg>> =

    /// <summary>命令对应的事件
    /// </summary>
    abstract member Convert : 'e