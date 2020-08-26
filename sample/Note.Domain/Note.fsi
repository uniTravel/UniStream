namespace Note

open System
open Note.Contract


/// <summary>Note聚合模块
/// </summary>
[<RequireQualifiedAccess>]
module Note =

    /// <summary>聚合值类型
    /// </summary>
    type Value =
        { Title: string
          Content: string
          Count: int }

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
        member ApplyEvent : (string -> ReadOnlyMemory<byte> -> T)

        /// <summary>聚合值
        /// </summary>
        member Value : Value

        /// <summary>聚合是否已关闭
        /// </summary>
        member Closed : bool

    /// <summary>创建Note
    /// </summary>
    /// <param name="cv">领域命令值。</param>
    /// <param name="agg">当前聚合。</param>
    val internal createNote :
        cv: CreateNote ->
        agg: T ->
        ((string * ReadOnlyMemory<byte>) seq * T)

    /// <summary>改变Note
    /// </summary>
    /// <param name="cv">领域命令值。</param>
    /// <param name="agg">当前聚合。</param>
    val internal changeNote :
        cv: ChangeNote ->
        agg: T ->
        ((string * ReadOnlyMemory<byte>) seq * T)