namespace UniStream.Infrastructure.EventStore

open System
open EventStore.Client


/// <summary>领域日志访问者模块
/// <para>领域日志流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainLog =

    /// <summary>写入领域日志
    /// <para>Stream名称：领域上下文-用户。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="ctx">带连字符‘-’的领域上下文。</param>
    /// <param name="user">用户。</param>
    /// <param name="category">领域日志类别。</param>
    /// <param name="data">领域日志数据。</param>
    /// <param name="metadata">领域日志元数据。</param>
    val write :
        client: EventStoreClient ->
        ctx: string ->
        user: string ->
        category: string ->
        data: ReadOnlyMemory<byte> ->
        metadata: Nullable<ReadOnlyMemory<byte>> ->
        Async<unit>