namespace Note.Domain

open Note.Contract


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
    /// <param name="cv">领域命令值。</param>
    /// <param name="t">当前聚合。</param>
    val internal createNote : CreateNote -> T -> ((string * byte[])[] * T)

    /// <summary>改变Note
    /// </summary>
    /// <param name="cv">领域命令值。</param>
    /// <param name="t">当前聚合。</param>
    val internal changeNote : ChangeNote -> T -> ((string * byte[])[] * T)