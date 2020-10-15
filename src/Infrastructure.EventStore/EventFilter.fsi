namespace UniStream.Infrastructure.EventStore

open System
open EventStore.Client


/// <summary>领域事件过滤器模块
/// </summary>
[<RequireQualifiedAccess>]
module EventFilter =

    /// <summary>领域事件过滤器
    /// </summary>
    type T

    /// <summary>创建领域事件过滤器
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    val create :
        client: EventStoreClient ->
        T

    /// <summary>订阅领域事件流
    /// <para>按条件过滤出领域事件流。</para>
    /// </summary>
    /// <param name="filter">过滤条件。</param>
    /// <param name="positon">订阅的起始位置。</param>
    /// <param name="handler">领域事件处理函数。</param>
    val sub :
        T ->
        filter: SubscriptionFilterOptions ->
        position: Position ->
        handler: (string -> uint64 -> string -> ReadOnlyMemory<byte> -> Async<unit>) ->
        Async<unit>

    /// <summary>退订领域事件流
    /// </summary>
    /// <param name="filter">过滤条件。</param>
    val unsub :
        T ->
        filter: SubscriptionFilterOptions ->
        Async<unit>