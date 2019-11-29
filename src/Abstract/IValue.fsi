namespace UniStream.Abstract


/// <summary>领域值接口
/// <para>领域命令、领域事件的值类型需要实现该接口。</para>
/// </summary>
[<Interface>]
type IValue =
    interface end


/// <summary>领域值包装接口
/// <para>领域命令、领域事件需要实现该接口。</para>
/// </summary>
/// <typeparam name="'v">领域值类型。</typeparam>
[<Interface>]
type IWrapped<'v when 'v :> IValue> =

    /// <summary>领域值
    /// </summary>
    abstract member Value : 'v