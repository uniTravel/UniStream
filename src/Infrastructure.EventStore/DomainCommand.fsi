namespace UniStream.Infrastructure.EventStore

open System
open EventStore.Client


/// <summary>领域命令访问者模块
/// <para>领域命令流存储的EventStore实现。有两种领域命令：</para>
/// <para>1、聚合命令：只影响一个聚合。</para>
/// <para>2、流程命令：会影响多个聚合。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainCommand =

    /// <summary>写入领域命令
    /// <para>1、Stream名称：领域命令值类型全名。</para>
    /// <para>2、关联ID作为事件类型。</para>
    /// </summary>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <typeparam name="^v">领域命令响应类型。</typeparam>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="correlationId">关联ID：聚合命令为聚合键；流程命令为客户端标识。</param>
    /// <param name="cmd">领域命令。</param>
    /// <returns>聚合命令的执行结果。</returns>
    val inline launch :
        client: EventStoreClient ->
        correlationId: string ->
        cmd: ^c ->
        Async<Result< ^v, string>>
        when ^c : (static member FullName : string)
        and ^c : (member Raw : unit -> ReadOnlyMemory<byte>)

    /// <summary>连接到领域命令流服务端订阅
    /// <para>订阅实例在服务端，支持多节点的消费群组连接，用以并行消费。</para>
    /// <para>领域命令值全名作为Stream及消费群组名称。</para>
    /// </summary>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <typeparam name="^v">领域命令响应类型。</typeparam>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="subClient">EventStore持久化订阅客户端。</param>
    /// <param name="handler">领域命令处理函数。</param>
    val inline subscribe< ^c, ^v> :
        client: EventStoreClient ->
        subClient: EventStorePersistentSubscriptionsClient ->
        handler: (string -> string -> string -> ReadOnlyMemory<byte> -> (^v -> unit) -> Async<unit>) ->
        Async<SubscriptionDroppedReason * exn>
        when ^c : (static member FullName : string)
        and ^c : (member Raw : unit -> ReadOnlyMemory<byte>)