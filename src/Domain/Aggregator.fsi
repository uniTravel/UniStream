namespace UniStream.Domain

open System


[<RequireQualifiedAccess>]
module Aggregator =

    /// <summary>聚合仓储访问类型
    /// </summary>
    type Accessor<'agg> =
        | Take of Guid * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of Guid * 'agg * int64
        | Scavenge of int64

    /// <summary>存储配置
    /// </summary>
    /// <param name="Get">获取聚合全部事件的函数。</param>
    /// <param name="GetFrom">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="EsFunc">领域事件流存储函数。</param>
    /// <param name="LdFunc">领域日志流存储函数。</param>
    /// <param name="LgFunc">诊断日志流存储函数。</param>
    type StoreConfig =
        { Get: string -> Guid -> (Guid * string * byte[])[] * int64
          GetFrom: string -> Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: string -> Guid -> int64 -> Guid -> string -> byte[] -> unit
          LdFunc: string -> Guid -> string -> byte[] -> unit
          LgFunc: string -> byte[] -> unit }

    /// <summary>聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="AggType">获取聚合全部事件的函数。</param>
    /// <param name="Timeout">聚合的超时Ticks约束。</param>
    /// <param name="DomainLog">领域日志记录器。</param>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="Get">获取聚合全部事件的函数。</param>
    /// <param name="GetFrom">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="EsFunc">领域事件流存储函数。</param>
    /// <param name="Agent">聚合仓储访问代理。</param>
    type T<'agg> =
        { AggType: string
          Timeout: int64
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> (Guid * string * byte[])[] * int64
          GetFrom: Guid -> int64 -> (Guid * string * byte[])[] * int64
          EsFunc: Guid -> int64 -> Guid -> string -> byte[] -> unit
          Agent: MailboxProcessor<Accessor<'agg>> }

    /// <summary>创建存储配置
    /// </summary>
    /// <param name="get">获取聚合全部事件的函数。</param>
    /// <param name="getFrom">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="esFunc">领域事件流存储函数。</param>
    /// <param name="ldFunc">领域日志流存储函数。</param>
    /// <param name="lgFunc">诊断日志流存储函数。</param>
    val config :
        (string -> Guid -> (Guid * string * byte[])[] * int64) ->
        (string -> Guid -> int64 -> (Guid * string * byte[])[] * int64) ->
        (string -> Guid -> int64 -> Guid -> string -> byte[] -> unit) ->
        (string -> Guid -> string -> byte[] -> unit) ->
        (string -> byte[] -> unit) -> StoreConfig

    /// <summary>创建聚合仓储访问代理
    /// </summary>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="get">获取聚合全部事件的函数。</param>
    /// <param name="timeout">聚合的超时Ticks约束。</param>
    val inline agent : DiagnoseLog.Logger -> (Guid -> (Guid * string * byte[])[] * int64) -> int64 -> MailboxProcessor<Accessor< ^agg>>
        when ^agg : (static member Empty : ^agg)
        and ^agg : (member Apply : (string -> byte[] -> ^agg))

    /// <summary>创建聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="cfg">存储配置。</param>
    /// <param name="blockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
    val inline create : StoreConfig -> int64 -> T< ^agg >
        when ^agg : (static member Empty : ^agg)
        and ^agg : (member Apply : (string -> byte[] -> ^agg))

    /// <summary>应用命令
    /// <para>应用命令的内部实现。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="applyEvent">应用事件于聚合的函数。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="deltaType">边际影响类型。</param>
    /// <param name="deltaBytes">边际影响的UTF8字节数组。</param>
    val inline apply : T< ^agg> -> (^agg -> ^agg) -> Guid -> Guid -> string -> byte[] -> Async<unit>
        when ^agg : (member Apply : (string -> byte[] -> ^agg))

    /// <summary>应用命令
    /// <para>面向流程管理器传入的命令。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="command">领域命令。</param>
    val inline applyCommand : T< ^agg> -> Guid -> Guid -> ^c -> Async<unit>
        when ^agg : (member Apply : (string -> byte[] -> ^agg))
        and ^c : (member Value: 'a)
        and ^c : (member ApplyEvent: (^agg -> ^agg))

    /// <summary>应用命令
    /// <para>面向网络传入、数组格式的原始命令。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <typeparam name="^d">边际影响类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="deltaType">边际影响类型。</param>
    /// <param name="deltaBytes">边际影响的UTF8字节数组。</param>
    /// <param name="commandCreator">创建命令的函数。</param>
    val inline applyRaw : T< ^agg> -> Guid -> Guid -> string -> byte[] -> (^d -> ^c) -> Async<unit>
        when ^agg : (member Apply : (string -> byte[] -> ^agg))
        and ^c : (member Value: 'a)
        and ^c : (member ApplyEvent: (^agg -> ^agg))