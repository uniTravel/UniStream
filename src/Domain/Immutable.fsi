namespace UniStream.Domain

open System


/// <summary>不可变聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Immutable =

    /// <summary>不可变聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="AggType">聚合类型。</param>
    /// <param name="DomainLog">领域日志记录器。</param>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="EsFunc">领域事件流存储函数。</param>
    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          EsFunc: Guid -> int64 -> (string * byte[])[] -> byte[] -> Async<int64> }

    /// <summary>创建不可变聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="cfg">聚合器配置。</param>
    val inline create : Config.Immutable -> T< ^agg >

    /// <summary>执行命令
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="command">领域命令。</param>
    val inline apply : T< ^agg> -> string -> Guid -> Guid -> ^c -> Async< ^v>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member Value : ^v)
        and ^c : (static member ValueType : string)
        and ^c : (member Apply: (^agg -> (string * byte[])[] * ^agg))