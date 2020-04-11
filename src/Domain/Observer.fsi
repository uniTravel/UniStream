namespace UniStream.Domain


/// <summary>观察者聚合器模块
/// <para>通过聚合ID或关联Key连接事件流。</para>
/// </summary>
[<RequireQualifiedAccess>]
module Observer =

    /// <summary>聚合仓储访问类型
    /// </summary>
    type Repo<'agg> =
        | Take of string * AsyncReplyChannel<Result<'agg * int64, string>>
        | Put of string * 'agg * int64
        | Refresh of AsyncReplyChannel<Map<string, unit>>
        | Scavenge

    /// <summary>流存储订户访问类型
    /// </summary>
    type Sub =
        | Subscribe of string * SubDropHandler * AsyncReplyChannel<string voption>
        | Unsubscribe of string
        | Clean of Map<string, unit>

    /// <summary>观察者聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="DiagnoseLog">诊断日志记录器。</param>
    /// <param name="SubAgent">流存储订户访问代理。</param>
    /// <param name="RepoAgent">聚合仓储访问代理。</param>
    type T<'agg> =
        { DiagnoseLog: DiagnoseLog.Logger
          SubAgent: MailboxProcessor<Sub>
          RepoAgent: MailboxProcessor<Repo<'agg>> }

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
    /// <param name="aggregator">聚合器。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    val inline get : T< ^agg> -> string -> Async< ^v>
        when ^agg : (member Value : ^v)