namespace UniStream.Infrastructure.EventStore

open System
open EventStore.Client


/// <summary>领域事件订阅者模块
/// </summary>
[<RequireQualifiedAccess>]
module EventSubscriber =

    /// <summary>领域事件订阅者
    /// </summary>
    type T

    /// <summary>创建领域事件订阅者
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="handler">领域事件处理函数。</param>
    val create :
        client: EventStoreClient ->
        aggType: string ->
        handler: (string -> uint64 -> string -> ReadOnlyMemory<byte> -> Async<unit>) ->
        T

    /// <summary>订阅领域事件流
    /// </summary>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    /// <param name="positon">订阅的起始位置。</param>
    val sub :
        T ->
        aggKey: string ->
        position: StreamPosition ->
        Async<unit>

    /// <summary>退订领域事件流
    /// </summary>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    val unsub :
        T ->
        aggKey: string ->
        Async<unit>