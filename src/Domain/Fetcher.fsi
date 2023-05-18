namespace UniStream.Domain

open System


/// <summary>抓取器模块
/// </summary>
[<RequireQualifiedAccessAttribute>]
module Fetcher =

    /// <summary>消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    type Msg<'agg> =
        | Refresh
        | Get of Guid * uint64 * AsyncReplyChannel<Result<'agg, string>>

    /// <summary>聚合约束组
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    type Agg<'agg when 'agg :> Aggregate and 'agg: (member ReplayEvent: (string -> ReadOnlyMemory<byte> -> unit))> =
        'agg

    /// <summary>生成聚合抓取操作的代理
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="creator">创建初始聚合的函数。</param>
    /// <param name="fetcher">抓取领域事件流的函数。</param>
    /// <param name="capacity">聚合缓存的容量，应介于5000~1000000之间。</param>
    /// <param name="refresh">刷新聚合缓存的间隔，单位为秒，应介于1~7200秒之间。</param>
    /// <returns>新聚合</returns>
    val inline build:
        [<InlineIfLambda>] creator: (Guid -> 'agg) ->
        [<InlineIfLambda>] fetcher: (string -> uint64 -> uint64 -> Async<seq<string * ReadOnlyMemory<byte>>>) ->
        capacity: int ->
        refresh: float ->
            MailboxProcessor<Msg<'agg>>
            when Agg<'agg>

    /// <summary>获取特定版本的聚合
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="agent">处理聚合抓取操作的代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="revision">目标聚合版本。</param>
    /// <returns>取得的聚合</returns>
    val inline get: agent: MailboxProcessor<Msg<'agg>> -> aggId: Guid -> revision: uint64 -> Async<'agg> when Agg<'agg>
