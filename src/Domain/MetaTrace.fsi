namespace UniStream.Domain

open System


[<RequireQualifiedAccess>]
module MetaTrace =

    /// <summary>领域追踪元数据
    /// </summary>
    /// <param name="AggregateId">聚合ID。</param>
    /// <param name="TraceId">跟踪ID。</param>
    /// <param name="TypeName">事件类型全名。</param>
    type T

    /// <summary>获取领域追踪元数据的值
    /// </summary>
    /// <param name="meta">领域追踪元数据。</param>
    /// <returns>领域追踪元数据的值。</returns>
    val internal value : T -> {| AggregateId: Guid; TraceId: Guid; TypeName: string |}

    /// <summary>创建领域追踪元数据
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>领域追踪元数据。</returns>
    val internal create<'v when 'v :> IValue> : Guid -> T

    /// <summary>转成字节数组
    /// <para>领域追踪元数据采用二进制序列化。</para>
    /// </summary>
    /// <param name="meta">领域追踪元数据。</param>
    /// <returns>领域追踪元数据的字节数组。</returns>
    val internal asBytes : T -> byte[]

    /// <summary>转成领域追踪元数据
    /// <para>领域追踪元数据采用二进制反序列化。</para>
    /// </summary>
    /// <param name="bytes">领域追踪元数据的字节数组。</param>
    /// <returns>领域追踪元数据。</returns>
    val internal fromBytes : byte[] -> T