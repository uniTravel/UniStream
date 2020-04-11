namespace UniStream.Infrastructure

open System
open EventStore.ClientAPI


/// <summary>领域事件访问者模块
/// <para>领域事件流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainEvent =

    /// <summary>从某个版本开始为聚合获取事件流
    /// <para>可选择依据聚合ID或者关联Key获取聚合事件：</para>
    /// <para>1、依据聚合ID的，以“聚合类型-聚合ID”作为Stream名称。</para>
    /// <para>2、依据关联Key的，以“$bc-关联Key”作为Stream名称。</para>
    /// <para>3、如果起始版本为0，则取出全部聚合事件。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="prerix">流名称前缀：聚合类型或$bc。</param>
    /// <param name="id">流名称ID：聚合ID或关联Key。</param>
    /// <param name="version">起始事件版本。</param>
    /// <returns>同一聚合ID下、从某个版本开始的领域事件的有序集合与当前版本号。</returns>
    val get : IEventStoreConnection -> string -> string -> int64 -> ((string * byte[])[] * int64)

    /// <summary>写入领域事件
    /// <para>聚合类型-聚合ID作为Stream名称。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="version">事件版本。</param>
    /// <param name="eData">事件数据。</param>
    /// <returns>当前版本号。</returns>
    val write : IEventStoreConnection -> string -> string -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>

    /// <summary>领域事件流订阅
    /// <para>依据聚合键的客户端订阅。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="prerix">流名称前缀：聚合类型或$bc。</param>
    /// <param name="id">流名称ID：聚合ID或关联Key。</param>
    /// <param name="handler">事件处理函数。</param>
    /// <param name="dropHandler">订阅被删除的善后处理函数。</param>
    /// <returns>取消订阅的函数。</returns>
    val subscribe :
        IEventStoreConnection ->
        string ->
        string ->
        (string -> string -> int64 -> byte[] -> byte[] -> Async<unit>) ->
        (string -> exn -> Async<unit>) ->
        (unit -> unit)

    /// <summary>领域事件流名称订阅
    /// <para>依据流名称的客户端订阅。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="streamName">流名称。</param>
    /// <param name="handler">订阅事件处理函数。</param>
    /// <param name="dropHandler">订阅被删除的善后处理函数。</param>
    /// <returns>取消订阅的函数。</returns>
    val streamSubscribe :
        IEventStoreConnection ->
        string ->
        (string -> string -> int64 -> byte[] -> byte[] -> Async<unit>) ->
        (string -> exn -> Async<unit>) ->
        (unit -> unit)

    /// <summary>连接到领域事件流订阅
    /// <para>服务端订阅，支持多节点的消费群组连接，用以并行消费。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="streamName">流名称。</param>
    /// <param name="groupName">消费群组名称。</param>
    /// <param name="handler">事件处理函数。</param>
    val connectSubscription :
        IEventStoreConnection ->
        string -> string ->
        (string -> string -> int64 -> byte[] -> byte[] -> Async<unit>) ->
        unit


/// <summary>领域命令访问者模块
/// <para>领域命令流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainCommand =

    /// <summary>写入领域命令
    /// <para>1、领域命令值类型全名作为Stream名称。</para>
    /// <para>2、聚合ID作为事件类型。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="cvType">领域命令值类型。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="data">领域命令数据。</param>
    /// <param name="metadata">领域事件元数据。</param>
    val write : IEventStoreConnection -> string -> Guid -> byte[] -> byte[] -> Async<unit>

    /// <summary>连接到领域命令流服务端订阅
    /// <para>订阅实例在服务端，支持多节点的消费群组连接，用以并行消费。</para>
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="cvType">领域命令值类型。</param>
    /// <param name="groupName">消费群组名称。</param>
    /// <param name="handler">命令处理函数。</param>
    val connectSubscription : IEventStoreConnection -> string -> string -> (string -> byte[] -> byte[] -> Async<unit>) -> unit


/// <summary>领域日志访问者模块
/// <para>领域日志流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainLog =

    /// <summary>写入领域日志
    /// <para>领域上下文加用户作为Stream名称。</para>
    /// </summary>
    /// <param name="ctx">领域上下文。</param>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="user">用户。</param>
    /// <param name="category">领域日志类别。</param>
    /// <param name="data">领域日志数据。</param>
    /// <param name="metadata">领域日志元数据。</param>
    val write : string -> IEventStoreConnection -> string -> string -> byte[] -> byte[] -> unit


/// <summary>诊断日志访问者模块
/// <para>诊断日志流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DiagnoseLog =

    /// <summary>写入诊断日志
    /// <para>领域上下文作为Stream名称。</para>
    /// </summary>
    /// <param name="ctx">领域上下文。</param>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="data">诊断日志数据。</param>
    val write : string -> IEventStoreConnection -> string -> byte[] -> unit