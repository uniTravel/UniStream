namespace UniStream.Domain

open System

[<RequireQualifiedAccess>]
module MetaEvent =

    /// <summary>领域事件元数据
    /// </summary>
    /// <param name="AggregateId">聚合ID。</param>
    /// <param name="TraceId">跟踪ID。</param>
    /// <param name="Version">事件版本。</param>
    type T

    /// <summary>获取领域事件元数据的值
    /// </summary>
    /// <param name="metaEvent">领域事件元数据。</param>
    /// <returns>领域事件元数据的值。</returns>
    val value : T -> {| AggregateId: Guid; TraceId: Guid; Version: int |}

    /// <summary>创建领域事件元数据
    /// </summary>
    /// <param name="metaLog">领域日志元数据。</param>
    /// <param name="version">事件版本。</param>
    /// <returns>领域事件元数据。</returns>
    val internal create : MetaLog.T -> int -> T

    /// <summary>转成字节数组
    /// <para>领域事件元数据与数据采用二进制序列化。</para>
    /// </summary>
    /// <param name="metaEvent">领域事件元数据。</param>
    /// <returns>领域事件元数据的字节数组。</returns>
    val asBytes : T -> byte[]

    /// <summary>转成领域事件元数据
    /// <para>领域事件元数据与数据采用二进制反序列化。</para>
    /// </summary>
    /// <param name="bytes">领域事件元数据的字节数组。</param>
    /// <returns>领域事件元数据。</returns>
    val fromBytes : byte[] -> T