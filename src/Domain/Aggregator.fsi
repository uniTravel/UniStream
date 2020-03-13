namespace UniStream.Domain

open System


/// <summary>聚合模式
/// <para>用以区分聚合在创建之后，是否还能更新状态。</para>
/// </summary>
/// <typeparam name="Mutable">可变模式。</typeparam>
/// <typeparam name="Immutable">不可变模式。</typeparam>
type AggMode = Mutable | Immutable

/// <summary>仓储模式
/// </summary>
/// <typeparam name="General">一般模式。</typeparam>
/// <typeparam name="Snapshot">快照模式。</typeparam>
type RepoMode = General | Snapshot of int64

/// <summary>聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Aggregator =

    /// <summary>聚合仓储访问类型
    /// </summary>
    type Accessor<'agg> =
        | Take of Guid * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of Guid * 'agg * int64
        | Refresh of int64
        | Scavenge of int64

    /// <summary>存储配置
    /// </summary>
    /// <param name="Get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="EsFunc">领域事件流存储函数。</param>
    /// <param name="LdFunc">领域日志流存储函数。</param>
    /// <param name="LgFunc">诊断日志流存储函数。</param>
    type StoreConfig =
        { Get: string -> Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: string -> Guid -> int64 -> (string * byte[])[] -> byte[] -> int64
          LdFunc: string -> string -> byte[] -> byte[] -> unit
          LgFunc: string -> byte[] -> unit }

    /// <summary>聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="AggType">获取聚合全部事件的函数。</param>
    /// <param name="Timeout">聚合的超时Ticks约束。</param>
    /// <param name="DomainLog">领域日志记录器。</param>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="Get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="EsFunc">领域事件流存储函数。</param>
    /// <param name="Agent">聚合仓储访问代理。</param>
    type T<'agg> =
        { AggType: string
          Timeout: int64
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: Guid -> int64 -> (string * byte[])[] -> byte[] -> int64
          Agent: MailboxProcessor<Accessor<'agg>> }

    /// <summary>创建存储配置
    /// </summary>
    /// <param name="get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="esFunc">领域事件流存储函数。</param>
    /// <param name="ldFunc">领域日志流存储函数。</param>
    /// <param name="lgFunc">诊断日志流存储函数。</param>
    val config :
        (string -> Guid -> int64 -> (Guid * string * byte[])[] * int64) ->
        (string -> Guid -> int64 -> (string * byte[])[] -> byte[] -> int64) ->
        (string -> string -> byte[] -> byte[] -> unit) ->
        (string -> byte[] -> unit) -> StoreConfig

    /// <summary>创建聚合仓储访问代理
    /// </summary>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="timeout">聚合的超时Ticks约束。</param>
    /// <param name="repoMode">仓储模式。</param>
    val inline agent : DiagnoseLog.Logger -> (Guid -> int64 -> (Guid * string * byte[])[] * int64) -> int64 -> RepoMode -> MailboxProcessor<Accessor< ^agg>>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>创建聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="cfg">存储配置。</param>
    /// <param name="blockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
    /// <param name="repoMode">仓储模式。</param>
    val inline create : StoreConfig -> int64 -> RepoMode -> T< ^agg >
        when ^agg : (static member Initial : ^agg)
        and ^agg : (static member AggMode : AggMode)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>执行命令
    /// <para>执行命令的内部实现。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="aggMode">聚合模式。</param>
    /// <param name="apply">应用命令的函数。</param>
    /// <param name="user">用户。</param>
    /// <param name="cvType">领域命令值类型。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    val inline execute : T< ^agg> -> AggMode -> (^agg -> (string * byte[])[] * ^agg) -> string -> string -> Guid -> Guid -> Async< ^v>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))
        and ^agg : (member Value : ^v)

    /// <summary>执行命令
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="command">领域命令。</param>
    val inline executeCommand : T< ^agg> -> string -> Guid -> Guid -> ^c -> Async< ^v>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (static member AggMode : AggMode)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))
        and ^agg : (member Value : ^v)
        and ^c : (static member ValueType : string)
        and ^c : (member Apply: (^agg -> (string * byte[])[] * ^agg))