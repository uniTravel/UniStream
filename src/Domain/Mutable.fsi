namespace UniStream.Domain

open System


/// <summary>可变聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Mutable =

    /// <summary>可变聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="AggType">聚合类型。</param>
    /// <param name="DomainLog">领域日志记录器。</param>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="Get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="EsFunc">领域事件流存储函数。</param>
    /// <param name="RepoAgent">聚合仓储访问代理。</param>
    /// <param name="BatAgent">批处理代理。</param>
    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Get
          EsFunc: EsFunc
          RepoAgent: MailboxProcessor<Repo<'agg>>
          BatAgent: MailboxProcessor<Bat<'agg>> }

    /// <summary>创建可变聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="cfg">聚合器配置。</param>
    val inline create : Config.Mutable -> T< ^agg >
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>执行命令
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="command">领域命令。</param>
    val inline apply : T< ^agg> -> string -> Guid -> Guid -> ^c -> Async< ^v>
        when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))
        and ^agg : (member Value : ^v)
        and ^c : (static member ValueType : string)
        and ^c : (member Apply: (^agg -> byte[] -> Result<(string * byte[] * byte[]) seq * ^agg, string>))

    /// <summary>批量执行命令
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="command">领域命令。</param>
    val inline batchApply : T< ^agg> -> string -> Guid -> Guid -> ^c -> Async<unit>
        when ^c : (static member ValueType : string)
        and ^c : (member Apply: (^agg -> byte[] -> Result<(string * byte[] * byte[]) seq * ^agg, string>))

    /// <summary>获取可变聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="aggId">聚合ID。</param>
    val inline get : T< ^agg> ->Guid -> Async< ^v>
        when ^agg : (member Value : ^v)