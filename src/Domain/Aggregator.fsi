namespace UniStream.Domain

open System


/// <summary>聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Aggregator =

    /// <summary>消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    type Msg<'agg> =
        | Refresh
        | Init of string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Apply of Guid * uint64 * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Correct of Guid * uint64 * AsyncReplyChannel<Result<'agg, string>>

    /// <summary>聚合约束组
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    type Agg<'agg
        when 'agg :> Aggregate
        and 'agg: (member ApplyCommand: (string -> ReadOnlyMemory<byte> -> string * ReadOnlyMemory<byte>))
        and 'agg: (member ReplayEvent: (string -> ReadOnlyMemory<byte> -> unit))> = 'agg

    /// <summary>生成聚合操作的代理
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="creator">创建初始聚合的函数。</param>
    /// <param name="writer">领域事件写入流存储的函数。</param>
    /// <param name="reader">读取领域事件流的函数。</param>
    /// <param name="capacity">聚合缓存的容量，应介于5000~1000000之间。</param>
    /// <param name="refresh">刷新聚合缓存的间隔，单位为秒，应介于1~7200秒之间。</param>
    /// <returns>新聚合</returns>
    val inline build:
        [<InlineIfLambda>] creator: (Guid -> 'agg) ->
        [<InlineIfLambda>] writer: (string -> uint64 -> string -> ReadOnlyMemory<byte> -> Async<unit>) ->
        [<InlineIfLambda>] reader: (string -> uint64 -> Async<seq<string * ReadOnlyMemory<byte>>>) ->
        capacity: int ->
        refresh: float ->
            MailboxProcessor<Msg<'agg>>
            when Agg<'agg>

    /// <summary>执行初始化聚合的领域命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="agent">处理聚合操作的代理。</param>
    /// <param name="cmType">领域命令类型。</param>
    /// <param name="cm">领域命令。</param>
    /// <returns>新聚合</returns>
    val inline init:
        agent: MailboxProcessor<Msg<'agg>> -> cmType: string -> cm: ReadOnlyMemory<byte> -> Async<'agg> when Agg<'agg>

    /// <summary>对已有聚合执行领域命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="agent">处理聚合操作的代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="revision">聚合版本。</param>
    /// <param name="cmType">领域命令类型。</param>
    /// <param name="cm">领域命令。</param>
    /// <returns>聚合</returns>
    val inline apply:
        agent: MailboxProcessor<Msg<'agg>> ->
        aggId: Guid ->
        revision: uint64 ->
        cmType: string ->
        cm: ReadOnlyMemory<byte> ->
            Async<'agg>
            when Agg<'agg>

    /// <summary>改正对已有聚合的错误操作
    /// <para>一个单元操作涉及多个聚合时，若其中一个失败，则其他的需要改正。</para>
    /// </summary>
    /// <remarks>业务约束：
    /// <para>1、只能针对聚合的最后一个版本改正。</para>
    /// <para>2、改正后，除版本外，内容与之前一个版本一致。</para>
    /// </remarks>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="agent">处理聚合操作的代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="revision">出错的聚合版本。</param>
    /// <returns>改正后的聚合</returns>
    val inline correct:
        agent: MailboxProcessor<Msg<'agg>> -> aggId: Guid -> revision: uint64 -> Async<'agg> when Agg<'agg>
