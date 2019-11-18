namespace UniStream.Domain

open System

[<RequireQualifiedAccess>]
module MetaLog =

    /// <summary>领域日志元数据
    /// </summary>
    /// <param name="AggregateId">聚合ID。</param>
    /// <param name="TraceId">跟踪ID。</param>
    type T

    /// <summary>获取领域日志元数据的值
    /// </summary>
    /// <param name="metaLog">领域日志元数据。</param>
    /// <returns>领域日志元数据的值。</returns>
    val value : T -> {| AggregateId: Guid; TraceId: Guid |}

    /// <summary>创建领域日志元数据
    /// </summary>
    /// <returns>领域日志元数据。</returns>
    val create : unit -> T

    /// <summary>转成字节数组
    /// <para>领域日志元数据与数据采用二进制序列化。</para>
    /// </summary>
    /// <param name="metaLog">领域日志元数据。</param>
    /// <returns>领域日志元数据的字节数组。</returns>
    val asBytes : T -> byte[]

    /// <summary>转成领域日志元数据
    /// <para>领域日志元数据与数据采用二进制反序列化。</para>
    /// </summary>
    /// <param name="bytes">领域日志元数据的字节数组。</param>
    /// <returns>领域日志元数据。</returns>
    val fromBytes : byte[] -> T