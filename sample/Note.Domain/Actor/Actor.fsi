namespace Note.Domain


/// <summary>聚合值类型
/// </summary>
type Actor =
    { Name: string }


/// <summary>Actor聚合模块
/// </summary>
[<RequireQualifiedAccess>]
module Actor =

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

        /// <summary>聚合值
        /// </summary>
        member Value : Actor

    /// <summary>创建Actor
    /// </summary>
    /// <param name="ev">领域事件值。</param>
    /// <param name="t">当前聚合。</param>
    val internal createActor : ActorCreated -> T -> ((string * byte[])[] * T)