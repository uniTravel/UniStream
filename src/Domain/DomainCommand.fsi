namespace UniStream.Domain

open UniStream.Abstract


[<RequireQualifiedAccess>]
module DomainCommand =

    /// <summary>创建领域命令
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'c">领域命令类型。</typeparam>
    /// <typeparam name="'e">领域事件类型。</typeparam>
    /// <param name="isValid">值验证函数。</param>e
    /// <param name="ctor">构造领域命令的函数。</param>e
    /// <param name="c">领域命令值。</param>
    /// <returns>领域事件。</returns>
    val create<'v, 'agg, 'c, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg> and 'c :> IDomainCommand<'v, 'agg, 'e>> :
        ('v -> bool) -> ('v -> 'c) -> 'v -> 'c

    /// <summary>应用函数
    /// <para>应用以领域命令值作为参数的函数，返回相应结果。</para>
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <typeparam name="'a">应用函数返回值的类型。</typeparam>a
    /// <param name="f">待应用的函数。</param>e
    /// <param name="c">领域命令值。</param>
    /// <returns>应用函数返回的结果。</returns>
    val apply<'v, 'a when 'v :> IValue> :
        ('v -> 'a) -> IWrapped<'v> -> 'a