namespace Note.Domain

open System


[<CLIMutable>]
type CreateActorCommand = { Name: string }

[<CLIMutable>]
type ActorValue = { Name: string; Sex: string }

[<CLIMutable>]
type ActorCreated = { Name: string }


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
        /// <para>根据领域事件类型，由领域事件流重建聚合。</para>
        /// </summary>
        member ApplyEvent : (string -> ReadOnlyMemory<byte> -> T)

        /// <summary>聚合值
        /// </summary>
        member Value : ActorValue

        /// <summary>聚合是否已关闭
        /// </summary>
        member Closed : bool

    /// <summary>创建Actor
    /// </summary>
    /// <param name="cv">领域命令值。</param>
    /// <param name="agg">当前聚合。</param>
    /// <returns>领域事件序列及新聚合。</returns>
    val internal createActor :
        cv: CreateActorCommand ->
        agg: T ->
        ((string * ReadOnlyMemory<byte>) seq * T)