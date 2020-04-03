namespace Note.Domain


[<CLIMutable>]
type ActorCreated = { Name: string }


/// <summary>Actor聚合模块
/// </summary>
[<RequireQualifiedAccess>]
module Actor =

    /// <summary>聚合值类型
    /// </summary>
    type Value = { Name: string }

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
        member Value : Value

    /// <summary>创建Actor
    /// </summary>
    /// <param name="ev">领域事件值。</param>
    /// <param name="agg">当前聚合。</param>
    /// <param name="metadata">领域事件元数据。</param>
    val internal createActor : ActorCreated -> T -> byte[] -> (string * byte[] * byte[]) seq * T