namespace UniStream.Domain

open System


[<RequireQualifiedAccess>]
module MetaTrace =

    /// <summary>领域追踪元数据
    /// </summary>
    type T

    /// <summary>创建领域追踪元数据
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="deltaType">边际影响类型全名。</param>
    /// <returns>领域追踪元数据。</returns>
    val internal createImpl : Guid -> string -> T

    /// <summary>创建领域追踪元数据
    /// </summary>
    /// <typeparam name="^d">边际影响类型。</typeparam>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>领域追踪元数据。</returns>
    val inline internal create< ^d> : Guid -> T
        when ^d : (static member DeltaType: string)

    /// <summary>转成字节数组
    /// <para>领域追踪元数据采用二进制序列化。</para>
    /// </summary>
    /// <param name="metaTrace">领域追踪元数据。</param>
    /// <returns>领域追踪元数据的字节数组。</returns>
    val internal asBytes : T -> byte[]

    /// <summary>转成领域追踪元数据
    /// <para>领域追踪元数据采用二进制反序列化。</para>
    /// </summary>
    /// <param name="bytes">领域追踪元数据的字节数组。</param>
    /// <returns>领域追踪元数据。</returns>
    val internal fromBytes : byte[] -> T

    type T with
        member internal AggregateId: Guid
        member internal TraceId: Guid
        member internal DeltaType: string