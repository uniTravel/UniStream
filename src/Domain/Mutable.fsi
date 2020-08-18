namespace UniStream.Domain

open System


/// <summary>可变聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Mutable =

    /// <summary>可变聚合器消息类型
    /// </summary>
    /// <param name="Apply">应用领域命令：聚合键*领域命令值类型全名*跟踪ID*领域命令数据*返回领域命令执行结果的管道。</param>
    /// <param name="Refresh">刷新缓存。</param>
    /// <param name="Scavenge">清扫快照。</param>
    /// <param name="Get">取出当前聚合。</param>
    type Msg<'agg> =
        | Apply of string * string * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Refresh
        | Scavenge
        | Get of string * AsyncReplyChannel<Result<'agg, string>>

    /// <summary>可变聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="DomainLog">领域日志记录器。</param>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="Agent">聚合器代理。</param>
    type T<'agg> =
        { DomainLog: DomainLog.T
          DiagnoseLog: DiagnoseLog.T
          Agent: MailboxProcessor<Msg<'agg>> }

    /// <summary>创建可变聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="cfg">聚合器配置。</param>
    val inline create :
        cfg: Config.Mutable ->
        T< ^agg >
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))
        and ^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg))
        and ^agg : (member Closed : bool)

    /// <summary>执行领域命令
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <typeparam name="^v">聚合值类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    /// <param name="cvType">领域命令值类型全名。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="data">领域命令数据。</param>
    val inline apply :
        aggregator: T< ^agg> ->
        user: string ->
        aggKey: string ->
        cvType: string ->
        traceId: string ->
        data: ReadOnlyMemory<byte> ->
        Async< ^v>
        when ^agg : (member Value : ^v)

    /// <summary>取出当前聚合
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <typeparam name="^v">聚合值类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    val inline get :
        aggregator: T< ^agg> ->
        aggKey: string ->
        Async< ^v>
        when ^agg : (member Value : ^v)