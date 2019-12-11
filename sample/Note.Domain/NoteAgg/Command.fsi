namespace Note.Domain.NoteAgg


[<RequireQualifiedAccess>]
module CreateNote =

    /// <summary>创建Note命令
    /// </summary>
    [<Sealed>]
    type T

    /// <summary>创建命令的函数
    /// </summary>
    /// <param name="command">命令值。</param>
    /// <returns>命令值。</returns>
    val create : (Create -> T)


[<RequireQualifiedAccess>]
module ChangeNote =

    /// <summary>更改Note命令
    /// </summary>
    [<Sealed>]
    type T

    /// <summary>创建命令的函数
    /// </summary>
    /// <param name="command">命令值。</param>
    /// <returns>命令值。</returns>
    val create : (Change -> T)