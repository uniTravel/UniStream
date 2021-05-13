namespace UniStream.Infrastructure.EventStore

open System
open System.Collections.Generic
open EventStore.Client


/// <summary>领域命令订阅者模块
/// </summary>
[<RequireQualifiedAccess>]
module CommandSubscriber =

    /// <summary>领域命令订阅者消息类型
    /// </summary>
    /// <typeparam name="Sub">订阅：订阅群组*持久化订阅。</typeparam>
    /// <typeparam name="Resub">重订。</typeparam>
    /// <typeparam name="Unsub">退订。</typeparam>
    type Msg =
        | Sub of string * PersistentSubscription
        | Resub of string * PersistentSubscription
        | Unsub of string

    /// <summary>领域命令订阅者
    /// </summary>
    /// <typeparam name="Client">EventStore客户端。</typeparam>
    /// <typeparam name="subClient">EventStore持久化订阅客户端。</typeparam>
    /// <typeparam name="Subs">领域命令订阅者集合。</typeparam>
    /// <typeparam name="Agent">领域命令订阅者代理。</typeparam>
    type T =
        { Client: EventStoreClient
          SubClient: EventStorePersistentSubscriptionsClient
          Subs: Dictionary<string, PersistentSubscription>
          Agent: MailboxProcessor<Msg> }

    /// <summary>创建领域命令订阅者
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="subClient">EventStore持久化订阅客户端。</param>
    val create :
        client: EventStoreClient ->
        subClient: EventStorePersistentSubscriptionsClient ->
        T

    /// <summary>连接到持久化的领域命令流订阅
    /// <para>订阅实例在服务端，支持多节点的消费群组连接，用以并行消费。</para>
    /// <para>领域命令值全名作为Stream名称。</para>
    /// </summary>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <typeparam name="^v">领域命令响应类型。</typeparam>
    /// <param name="group">订阅群组。</param>
    /// <param name="handler">领域命令处理函数。</param>
    val inline sub< ^c, ^v> :
        T ->
        group: string ->
        handler: (string -> string -> string -> string -> ReadOnlyMemory<byte> -> (^v -> unit) -> Async<unit>) ->
        Async<unit>
        when ^c : (static member FullName : string)

    /// <summary>退订持久化的领域命令流订阅
    /// </summary>
    /// <param name="group">订阅群组。</param>
    val unsub :
        T ->
        group: string ->
        Async<unit>