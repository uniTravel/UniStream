namespace UniStream.Domain

open System


/// <summary>聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Aggregator =

    /// <summary>消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    type Msg<'agg when Agg<'agg>> =
        | Refresh
        | Register of string * ('agg -> byte array -> unit)
        | Create of Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<Result<'agg, exn>>
        | Apply of Guid * Guid * ('agg -> unit) * ('agg -> string * byte array) * AsyncReplyChannel<Result<'agg, exn>>

    /// <summary>初始化聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="creator">聚合构造函数。</param>
    /// <param name="writer">流存储写入函数。</param>
    /// <param name="reader">流存储读取函数。</param>
    /// <param name="capacity">聚合缓存容量。</param>
    /// <param name="refresh">聚合缓存刷新间隔，单位秒。</param>
    /// <returns>聚合操作代理</returns>
    val inline init:
        [<InlineIfLambda>] creator: (Guid -> 'agg) ->
        writer: (Guid -> string -> Guid -> uint64 -> string -> byte array -> unit) ->
        reader: (string -> Guid -> seq<string * byte array>) ->
        capacity: int ->
        refresh: float ->
            MailboxProcessor<Msg<'agg>>
            when Agg<'agg>

    /// <summary>注册重播
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'rep">重播类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="rep">重播。</param>
    val inline register: agent: MailboxProcessor<Msg<'agg>> -> rep: 'rep -> unit when Rep<'agg, 'rep>

    /// <summary>创建聚合
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>新聚合</returns>
    val inline create:
        agent: MailboxProcessor<Msg<'agg>> -> traceId: Guid -> com: 'com -> Async<'agg> when Com<'agg, 'com, 'evt>

    /// <summary>变更聚合
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="traceId">追踪ID。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>聚合</returns>
    val inline apply:
        agent: MailboxProcessor<Msg<'agg>> -> traceId: Guid -> aggId: Guid -> com: 'com -> Async<'agg>
            when Com<'agg, 'com, 'evt>
