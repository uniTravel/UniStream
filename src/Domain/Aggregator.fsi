namespace UniStream.Domain

open System


[<RequireQualifiedAccess>]
module Aggregator =

    /// <summary>聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    type T<'agg>

    /// <summary>创建聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="get">重建聚合的函数。</param>
    /// <param name="esFunc">领域事件流存储函数。</param>
    /// <param name="ldFunc">领域日志流存储函数。</param>
    /// <param name="lgFunc">诊断日志流存储函数。</param>
    /// <param name="blockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
    /// <param name="aggType">领域类型全名。</param>
    val createImpl<'agg> :
        (string -> Guid -> (byte[] * byte[]) array) ->
        (string -> Guid -> string -> byte[] -> byte[] -> unit) ->
        (string -> Guid -> string -> byte[] -> byte[] -> unit) ->
        (string -> byte[] -> unit) ->
        int64 -> string -> T<'agg>

    /// <summary>创建聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="get">重建聚合的函数。</param>
    /// <param name="esFunc">领域事件流存储函数。</param>
    /// <param name="ldFunc">领域日志流存储函数。</param>
    /// <param name="lgFunc">诊断日志流存储函数。</param>
    /// <param name="blockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
    val inline create< ^agg> :
        (string -> Guid -> (byte[] * byte[]) array) ->
        (string -> Guid -> string -> byte[] -> byte[] -> unit) ->
        (string -> Guid -> string -> byte[] -> byte[] -> unit) ->
        (string -> byte[] -> unit) ->
        int64 -> T< ^agg >
        when ^agg : (static member AggType: string)

    /// <summary>应用命令
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="apply">应用命令于聚合的函数。</param>
    /// <param name="delta">边际影响。</param>
    /// <param name="t">聚合器。</param>
    /// <param name="metaTrace">领域追踪元数据。</param>
    val applyImpl : ('agg -> 'agg) -> byte[] -> T<'agg> -> MetaTrace.T -> Async<unit>

    /// <summary>应用命令
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="metaTrace">领域追踪元数据。</param>
    /// <param name="command">待执行的命令。</param>
    val inline apply : T<'agg> -> MetaTrace.T -> ^c -> Async<unit>
        when ^c : (member Value: 'd)
        and ^c : (member Apply: ('agg -> 'agg))