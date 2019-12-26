namespace UniStream.Infrastructure

open System


/// <summary>领域事件访问者模块
/// <para>领域事件流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainEvent =

    /// <summary>领域事件访问者
    /// </summary>
    type T

    /// <summary>创建领域事件访问者
    /// </summary>
    /// <param name="uri">EventStore连接Uri。</param>
    val create : Uri -> T

    /// <summary>从某个版本开始获取聚合事件
    /// <para>1、聚合类型-聚合ID作为Stream名称。</para>
    /// <para>2、如果起始版本为0，则取出全部聚合事件。</para>
    /// </summary>
    /// <param name="client">领域事件访问者。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="version">起始事件版本。</param>
    /// <returns>同一聚合ID下、从某个版本开始的领域事件的有序集合。</returns>
    val get : T -> string -> Guid -> int64 -> ((Guid * string * byte[])[] * int64)

    /// <summary>写入领域事件
    /// <para>聚合类型-聚合ID作为Stream名称。</para>
    /// </summary>
    /// <param name="client">领域事件访问者。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="version">事件版本。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="deltaType">边际影响类型。</param>
    /// <param name="delta">边际影响。</param>
    val write : T -> string -> Guid -> int64 -> Guid -> string -> byte[] -> unit


/// <summary>领域日志访问者模块
/// <para>领域日志流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainLog =

    /// <summary>领域日志访问者
    /// </summary>
    type T

    /// <summary>创建领域日志访问者
    /// </summary>
    /// <param name="uri">EventStore连接Uri。</param>
    val create : Uri -> T

    /// <summary>写入领域日志
    /// <para>聚合类型全名作为Stream名称。</para>
    /// </summary>
    /// <param name="client">领域日志访问者。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="status">业务状态。</param>
    /// <param name="dLog">领域日志数据。</param>
    val write : T -> string -> Guid -> string -> byte[] -> unit


/// <summary>诊断日志访问者模块
/// <para>诊断日志流存储的EventStore实现。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DiagnoseLog =

    /// <summary>诊断日志访问者
    /// </summary>
    type T

    /// <summary>创建诊断日志访问者
    /// </summary>
    /// <param name="uri">EventStore连接Uri。</param>
    /// <param name="stream">Stream名称。</param>
    val create : Uri -> string -> T

    /// <summary>写入诊断日志
    /// <para>领域上下文作为Stream名称。</para>
    /// </summary>
    /// <param name="client">诊断日志访问者。</param>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="gLog">诊断日志数据。</param>
    val write : T -> string -> byte[] -> unit