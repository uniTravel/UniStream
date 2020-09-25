namespace UniStream.Domain

open System


/// <summary>可变聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Mutable =

    /// <summary>可变聚合器消息类型
    /// </summary>
    /// <param name="Apply">应用领域命令：聚合键*跟踪ID*应用领域命令的函数*返回领域命令执行结果的管道。</param>
    /// <param name="Refresh">刷新缓存。</param>
    /// <param name="Scavenge">清扫快照。</param>
    /// <param name="Get">取出当前聚合。</param>
    type Msg<'agg> =
        | Apply of string * string * (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg) * AsyncReplyChannel<Result<'agg, string>>
        | Refresh
        | Scavenge
        | Get of string * AsyncReplyChannel<Result<'agg, string>>

    /// <summary>可变聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="Agent">聚合器代理。</param>
    type T<'agg> =
        { Agent: MailboxProcessor<Msg<'agg>> }

    /// <summary>创建可变聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="cfg">聚合器配置。</param>
    val inline create :
        cfg: Config.Mutable ->
        T< ^agg >
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))
        and ^agg : (member Closed : bool)

    /// <summary>执行领域命令
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <typeparam name="^v">聚合值类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cmd">领域命令。</param>
    val inline apply :
        aggregator: T< ^agg> ->
        aggKey: string ->
        traceId: string ->
        cmd: ^c ->
        Async< ^v>
        when ^agg : (member Value : ^v)
        and ^c : (member Apply : (^agg -> (string * ReadOnlyMemory<byte>) seq * ^agg))

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