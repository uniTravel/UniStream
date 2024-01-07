namespace UniStream.Domain

open System
open System.Collections.Generic


/// <summary>聚合器模块
/// </summary>
[<RequireQualifiedAccess>]
module Aggregator =

    /// <summary>消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    type Msg<'agg when Agg<'agg>> =
        | Refresh
        | Register of string * ('agg -> ReadOnlyMemory<byte> -> unit)
        | Create of ('agg -> unit) * ('agg -> string * ReadOnlyMemory<byte>) * AsyncReplyChannel<Result<'agg, exn>>
        | Apply of
            Guid *
            ('agg -> unit) *
            ('agg -> string * ReadOnlyMemory<byte>) *
            AsyncReplyChannel<Result<'agg, exn>>

    /// <summary>初始化聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="creator">创建初始聚合的函数。</param>
    /// <param name="writer">命令写入流存储的函数。</param>
    /// <param name="reader">读取命令流的函数。</param>
    /// <param name="capacity">聚合缓存的容量。</param>
    /// <param name="refresh">刷新聚合缓存的间隔，单位为秒。</param>
    /// <returns>聚合操作代理</returns>
    val inline init:
        [<InlineIfLambda>] creator: (Guid -> 'agg) ->
        [<InlineIfLambda>] writer: (string -> Guid -> uint64 -> string -> ReadOnlyMemory<byte> -> unit) ->
        [<InlineIfLambda>] reader: (string -> Guid -> seq<string * ReadOnlyMemory<byte>>) ->
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

    /// <summary>执行创建聚合的命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="com">命令。</param>
    /// <returns>新聚合</returns>
    val inline create: agent: MailboxProcessor<Msg<'agg>> -> com: 'com -> Async<'agg> when Com<'agg, 'com>

    /// <summary>对聚合执行命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <param name="agent">聚合操作代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    /// <returns>聚合</returns>
    val inline apply: agent: MailboxProcessor<Msg<'agg>> -> aggId: Guid -> com: 'com -> Async<'agg> when Com<'agg, 'com>
