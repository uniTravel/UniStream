namespace UniStream.Infrastructure.EventStore

open System
open System.Collections.Generic
open EventStore.Client


/// <summary>领域事件访问者模块
/// <para>领域事件流存储的EventStore实现。</para>
/// </summary>
/// <remarks>Stream名称：聚合类型-聚合ID。</remarks>
[<RequireQualifiedAccess>]
module DomainEvent =

    /// <summary>从某个版本开始为聚合获取事件流
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">带连字符‘-’的聚合类型。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="version">起始事件版本。</param>
    /// <returns>同一聚合ID下、从某个版本开始的领域事件的有序集合与当前版本号。</returns>
    val get :
        client: EventStoreClient ->
        aggType: string ->
        aggId: string ->
        version: uint64 ->
        IAsyncEnumerable<string * ReadOnlyMemory<byte> * ReadOnlyMemory<byte>>

    /// <summary>写入领域事件
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">带连字符‘-’的聚合类型。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="version">事件版本。</param>
    /// <param name="eData">事件数据。</param>
    val write :
        client: EventStoreClient ->
        aggType: string ->
        aggId: string ->
        version: uint64 ->
        eData: (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq ->
        Async<unit>

    /// <summary>订阅领域事件流
    /// <para>按条件过滤出领域事件流。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="filterType">过滤类型。</param>
    /// <param name="handler">事件处理函数。</param>
    val filter :
        client: EventStoreClient ->
        filterType: FilterType ->
        handler: (string -> string -> uint64 -> ReadOnlyMemory<byte> -> ReadOnlyMemory<byte> -> Async<unit>) ->
        Async<SubscriptionDroppedReason * exn>