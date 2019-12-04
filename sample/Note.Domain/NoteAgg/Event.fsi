namespace Note.Domain.NoteAgg

open UniStream.Domain


[<RequireQualifiedAccess>]
module NoteCreated =

    /// <summary>创建Note事件
    /// </summary>
    [<Sealed>]
    type T =
        interface IWrapped<CreateNote>
        interface IDomainEvent<CreateNote, Note.T>

    /// <summary>命令到事件的转换器
    /// </summary>
    /// <param name="wrap">包装的命令值。</param>
    /// <returns>事件值。</returns>
    val convert : IWrapped<CreateNote> -> T


[<RequireQualifiedAccess>]
module NoteChanged =

    /// <summary>更改Note事件
    /// </summary>
    [<Sealed>]
    type T =
        interface IWrapped<ChangeNote>
        interface IDomainEvent<ChangeNote, Note.T>

    /// <summary>命令到事件的转换器
    /// </summary>
    /// <param name="wrap">包装的命令值。</param>
    /// <returns>事件值。</returns>
    val convert : IWrapped<ChangeNote> -> T