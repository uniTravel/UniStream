namespace Note.Domain.NoteAgg

open UniStream.Domain


[<RequireQualifiedAccess>]
module CreateNote =

    /// <summary>创建Note命令
    /// </summary>
    [<Sealed>]
    type T =
        interface IWrapped<CreateNote>
        interface IDomainCommand<CreateNote, Note.T, NoteCreated.T>

    /// <summary>创建命令的函数
    /// </summary>
    /// <param name="command">命令值。</param>
    /// <returns>命令值。</returns>
    val create : (CreateNote -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    /// <summary>更改Note命令
    /// </summary>
    [<Sealed>]
    type T =
        interface IWrapped<ChangeNote>
        interface IDomainCommand<ChangeNote, Note.T, NoteChanged.T>

    /// <summary>创建命令的函数
    /// </summary>
    /// <param name="command">命令值。</param>
    /// <returns>命令值。</returns>
    val create : (ChangeNote -> T)