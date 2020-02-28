namespace Note.Domain


[<CLIMutable>]
type NoteCreated = { Title: string; Content: string }

[<CLIMutable>]
type NoteChanged = { Content: string }


/// <summary>Note聚合模块
/// </summary>
[<RequireQualifiedAccess>]
module Note =

    /// <summary>聚合值
    /// </summary>
    type private Value

    /// <summary>聚合类型
    /// </summary>
    type T
    type T with

        /// <summary>初始化聚合
        /// </summary>
        static member Initial : T

        /// <summary>应用领域事件
        /// <para>根据领域事件类型，由事件流重建聚合。</para>
        /// </summary>
        member ApplyEvent : (string -> byte[] -> T)

    /// <summary>创建Note
    /// </summary>
    /// <param name="ev">领域事件值。</param>
    /// <param name="t">当前聚合。</param>
    val internal createNote : NoteCreated -> T -> ((string * byte[])[] * T)

    /// <summary>改变Note
    /// </summary>
    /// <param name="ev">领域事件值。</param>
    /// <param name="t">当前聚合。</param>
    val internal changeNote : NoteChanged -> T -> ((string * byte[])[] * T)