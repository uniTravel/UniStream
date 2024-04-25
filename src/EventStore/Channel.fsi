namespace UniStream.Domain

open System
open EventStore.Client


/// <summary>命令通道模块
/// </summary>
[<RequireQualifiedAccess>]
module Channel =

    /// <summary>初始化命令通道
    /// </summary>
    /// <param name="client">EventStore客户端。</param>
    /// <returns>命令通道代理</returns>
    val init:
        client: IClient -> MailboxProcessor<string * Uuid * string * EventData * AsyncReplyChannel<Result<unit, exn>>>

    /// <summary>发送命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="agent">命令通道代理。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    val inline send<'agg, 'com, 'evt> :
        agent: MailboxProcessor<string * Uuid * string * EventData * AsyncReplyChannel<Result<unit, exn>>> ->
        aggId: Guid ->
        com: 'com ->
            Async<unit>
            when Com<'agg, 'com, 'evt>
