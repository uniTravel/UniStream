namespace UniStream.Domain

open System


/// <summary>观察者聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module ObServer =

    /// <summary>观察者聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="AggType">聚合类型。</param>
    /// <param name="DomainLog">领域日志记录器。</param>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="Get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="Agent">聚合仓储访问代理。</param>
    type T<'agg> =
        { AggType: string
          DomainLog: DomainLog.Logger
          DiagnoseLog: DiagnoseLog.Logger
          Get: Guid -> int64 -> (Guid * string * byte[])[] * int64
          Agent: MailboxProcessor<Accessor<'agg>> }

    /// <summary>创建聚合仓储访问代理
    /// </summary>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="repo">仓储模式。</param>
    /// <param name="blockTicks">聚合超时锁定Ticks约束。</param>
    val inline agent : DiagnoseLog.Logger -> (Guid -> int64 -> (Guid * string * byte[])[] * int64) -> RepoMode -> int64 -> MailboxProcessor<Accessor< ^agg>>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>更新观察者聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="evType">领域事件值类型。</param>
    /// <param name="number">领域事件版本。</param>
    /// <param name="data">领域事件数据。</param>
    /// <param name="matadata">领域事件元数据。</param>
    val inline update : T< ^agg> -> string -> Guid -> string -> int64 -> byte[] -> byte[] -> Async<unit>
        when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>创建观察者聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="cfg">聚合器配置。</param>
    val inline create : Config.Observer -> T< ^agg >
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>获取观察者聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="t">聚合器。</param>
    /// <param name="aggId">聚合ID。</param>
    val inline get : T< ^agg> ->Guid -> Async< ^v>
        when ^agg : (member Value : ^v)