namespace Note.Domain

open System
open Note.Contract


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

        /// <summary>应用领域命令
        /// </summary>
        member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * T)

        /// <summary>聚合值
        /// </summary>
        member Value : Actor

        /// <summary>聚合是否已关闭
        /// </summary>
        member Closed : bool