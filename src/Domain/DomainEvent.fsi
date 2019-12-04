namespace UniStream.Domain


[<RequireQualifiedAccess>]
module DomainEvent =

    /// <summary>创建领域事件
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'e">领域事件类型。</typeparam>
    /// <param name="ctor">构造领域事件的函数。</param>e
    /// <param name="e">领域事件值。</param>
    /// <returns>领域事件。</returns>
    val create<'v, 'agg, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg>> :
        ('v -> 'e) -> 'v -> 'e

    /// <summary>应用函数
    /// <para>应用以领域事件值作为参数的函数，返回相应结果。</para>
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <typeparam name="'a">应用函数返回值的类型。</typeparam>a
    /// <param name="f">待应用的函数。</param>e
    /// <param name="e">领域事件值。</param>
    /// <returns>应用函数返回的结果。</returns>
    val apply<'v, 'a when 'v :> IValue> :
        ('v -> 'a) -> IWrapped<'v> -> 'a

    /// <summary>转成UTF8字节数组
    /// <para>领域事件数据采用UTF8格式的Json序列化。</para>
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <param name="e">领域事件数据。</param>
    /// <returns>领域事件数据的UTF8字节数组。</returns>
    val internal asBytes<'v when 'v :> IValue> : 'v -> byte[]

    /// <summary>转成领域事件数据
    /// <para>领域事件数据采用UTF8格式的Json反序列化。</para>
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <param name="bytes">领域事件数据的UTF8字节数组。</param>
    /// <returns>领域事件数据。</returns>
    val internal fromBytes<'v when 'v :> IValue> : byte[] -> 'v