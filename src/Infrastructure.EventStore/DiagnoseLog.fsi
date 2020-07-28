namespace UniStream.Infrastructure.EventStore

open System
open EventStore.Client


/// <summary>诊断日志访问者模块
/// <para>诊断日志流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DiagnoseLog =

    /// <summary>写入诊断日志
    /// <para>Stream名称：领域上下文。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="ctx">领域上下文。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="data">诊断日志数据。</param>
    val write :
        client: EventStoreClient ->
        ctx: string ->
        aggType: string ->
        data: ReadOnlyMemory<byte> ->
        Async<unit>