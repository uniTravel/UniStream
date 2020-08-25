namespace UniStream.Infrastructure.EventStore

open System
open EventStore.Client


/// <summary>领域事件访问者模块
/// <para>领域事件流存储的EventStore实现。</para>
/// </summary>
/// <remarks>Stream名称：聚合类型-聚合键。</remarks>
[<RequireQualifiedAccess>]
module DomainEvent =

    /// <summary>从某个版本开始为聚合获取领域事件流
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">带连字符‘-’的聚合类型。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    /// <param name="version">起始领域事件版本。</param>
    /// <returns>同一聚合键下、从某个版本开始的领域事件的有序集合。</returns>
    val get :
        client: EventStoreClient ->
        aggType: string ->
        aggKey: string ->
        version: uint64 ->
        Async<(uint64 * string * ReadOnlyMemory<byte>) seq>

    /// <summary>写入领域事件
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">带连字符‘-’的聚合类型。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    /// <param name="version">领域事件版本。</param>
    /// <param name="eData">领域事件数据。</param>
    val write :
        client: EventStoreClient ->
        aggType: string ->
        aggKey: string ->
        version: uint64 ->
        eData: (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq ->
        Async<unit>

    /// <summary>订阅领域事件流
    /// <para>按条件过滤出领域事件流。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="filterType">过滤类型。</param>
    /// <param name="handler">领域事件处理函数。</param>
    val filter :
        client: EventStoreClient ->
        filterType: FilterType ->
        handler: (string -> string -> uint64 -> ReadOnlyMemory<byte> -> Async<unit>) ->
        Async<SubscriptionDroppedReason * exn>