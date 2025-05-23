namespace UniStream.Domain

open System
open System.Threading


/// <summary>聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Aggregator =

    /// <summary>消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    type Msg<'agg when 'agg :> Aggregate> =
        | Register of string * ('agg -> ReadOnlyMemory<byte> -> unit)
        | Create of Guid * Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<ComResult>
        | Apply of Guid * Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<ComResult>
        | Get of Guid * AsyncReplyChannel<Result<'agg, exn>>

    /// <summary>创建聚合
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>命令执行结果</returns>
    val inline create:
        agent: MailboxProcessor<Msg<'agg>> -> aggId: Guid -> comId: Guid -> com: 'com -> Async<ComResult>
            when Com<'agg, 'com, 'evt>

    /// <summary>变更聚合
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>命令执行结果</returns>
    val inline apply:
        agent: MailboxProcessor<Msg<'agg>> -> aggId: Guid -> comId: Guid -> com: 'com -> Async<ComResult>
            when Com<'agg, 'com, 'evt>

    /// <summary>获取聚合
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>聚合</returns>
    val inline get: agent: MailboxProcessor<Msg<'agg>> -> aggId: Guid -> Async<'agg> when 'agg :> Aggregate

    /// <summary>注册重播
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'rep">重播类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="rep">重播。</param>
    val inline register: agent: MailboxProcessor<Msg<'agg>> -> rep: 'rep -> unit when Rep<'agg, 'rep>

    /// <summary>初始化聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'stream">领域流配置类型。</typeparam>
    /// <param name="creator">聚合构造函数。</param>
    /// <param name="token">取消凭据。</param>
    /// <param name="stream">领域流配置。</param>
    /// <param name="options">聚合配置选项。</param>
    /// <returns>聚合操作代理</returns>
    val inline init:
        [<InlineIfLambda>] creator: (Guid -> 'agg) ->
        token: CancellationToken ->
        stream: 'stream ->
        options: AggregateOptions ->
            MailboxProcessor<Msg<'agg>>
            when 'agg :> Aggregate and 'stream :> IStream<'agg>
