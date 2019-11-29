namespace Note.Domain.NoteAgg

open UniStream.Abstract


[<RequireQualifiedAccess>]
module Note =

    /// <summary>Note聚合
    /// </summary>
    [<Sealed>]
    type T =
        interface IAggregate

    /// <summary>创建Note事件处理函数
    /// </summary>
    /// <param name="wrap">包装的事件值。</param>
    /// <param name="t">当前聚合值。</param>
    /// <returns>新聚合 * 版本 * 事件值。</returns>
    val noteCreated : IWrapped<CreateNote> -> T -> (T * int * CreateNote)

    /// <summary>更改Note内容事件处理函数
    /// </summary>
    /// <param name="wrap">包装的事件值。</param>
    /// <param name="t">当前聚合值。</param>
    /// <returns>新聚合 * 版本 * 事件值。</returns>
    val noteChanged : IWrapped<ChangeNote> -> T -> (T * int * ChangeNote)