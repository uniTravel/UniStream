namespace Note.Domain

open Note.Contract


/// <summary>Actor聚合模块
/// </summary>
[<RequireQualifiedAccess>]
module Actor =

    /// <summary>Actor聚合
    /// </summary>
    type T

    /// <summary>创建Actor
    /// </summary>
    /// <param name="delta">边际影响。</param>
    /// <param name="t">当前Actor聚合。</param>
    val internal actorCreated : CreateActor -> T -> T

    type T with

        /// <summary>初始化一个空的聚合
        /// </summary>
        static member Empty : T

        /// <summary>应用边际影响
        /// <para>根据边际影响类型，由事件流重建聚合。</para>
        /// </summary>
        member Apply : (string -> byte[] -> T)