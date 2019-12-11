namespace Note.Domain.NoteAgg


[<CLIMutable>]
type Create = { Title: string; Content: string }

[<CLIMutable>]
type Change = { Content: string }


[<RequireQualifiedAccess>]
module internal Note =

    /// <summary>Note聚合
    /// </summary>
    type T =
        | Init
        | Active of {| Title: string; Content: string |}

    /// <summary>创建Note事件处理函数
    /// </summary>
    /// <param name="c">应用的命令。</param>
    /// <param name="t">当前聚合值。</param>
    /// <returns>新聚合。</returns>
    val inline noteCreated : ^c -> T -> T
        when ^c : (member Value: Create)

    /// <summary>更改Note内容事件处理函数
    /// </summary>
    /// <param name="c">应用的命令。</param>
    /// <param name="t">当前聚合值。</param>
    /// <returns>新聚合。</returns>
    val inline noteChanged : ^c -> T -> T
        when ^c : (member Value: Change)