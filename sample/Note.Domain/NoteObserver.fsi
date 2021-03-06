namespace Note

open System


/// <summary>NoteObserver聚合模块
/// </summary>
[<RequireQualifiedAccess>]
module NoteObserver =

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