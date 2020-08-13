namespace UniStream.Domain

open System


/// <summary>不可变聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Immutable =

    /// <summary>不可变聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="DomainLog">领域日志记录器。</param>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="Writer">聚合事件流存储函数。</param>
    type T<'agg> =
        { DomainLog: DomainLog.T
          DiagnoseLog: DiagnoseLog.T
          Writer: string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit> }

    /// <summary>创建不可变聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="cfg">聚合器配置。</param>
    val inline create :
        cfg: Config.Immutable ->
        T< ^agg >

    /// <summary>执行命令
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <typeparam name="^v">聚合值类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="command">领域命令。</param>
    val inline apply :
        aggregator: T< ^agg> ->
        user: string ->
        aggKey: string ->
        traceId: string ->
        command: ^c ->
        Async< ^v>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member Value : ^v)
        and ^c : (static member FullName : string)
        and ^c : (member Apply: (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg))