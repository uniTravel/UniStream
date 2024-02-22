namespace UniStream.Infrastructure

open System
open EventStore.Client


/// <summary>Stream模块
/// </summary>
[<RequireQualifiedAccess>]
module Stream =

    /// <summary>EventStore客户端类型
    /// </summary>
    type T

    /// <summary>创建EventStore客户端
    /// </summary>
    /// <param name="settings">EventStore客户端设置。</param>
    /// <returns>EventStore客户端</returns>
    val create: settings: EventStoreClientSettings -> T

    /// <summary>关闭EventStore客户端
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    val close: client: T -> unit

    /// <summary>聚合事件写入流
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggType">聚合类型全称。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="revision">聚合版本。</param>
    /// <param name="evtType">事件类型。</param>
    /// <param name="evtData">事件数据。</param>
    val write:
        client: T ->
        traceId: Guid option ->
        aggType: string ->
        aggId: Guid ->
        revision: uint64 ->
        evtType: string ->
        evtData: byte array ->
            unit

    /// <summary>读取聚合事件流
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">聚合类型全称。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>聚合事件流</returns>
    val read: client: T -> aggType: string -> aggId: Guid -> (string * byte array) list
