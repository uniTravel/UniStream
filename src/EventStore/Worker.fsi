namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Threading
open Microsoft.Extensions.Logging
open EventStore.Client


/// <summary>命令处理后台模块
/// </summary>
[<RequireQualifiedAccess>]
module Worker =

    /// <summary>注册命令代理
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="logger">日志记录器。</param>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="f">处理流数据的回调函数。</param>
    /// <returns>命令代理</returns>
    val inline register<'agg, 'com, 'evt> :
        logger: ILogger ->
        client: IClient ->
        f: (Guid option -> Guid -> 'com -> Async<'agg>) ->
            string * MailboxProcessor<ResolvedEvent>
            when Com<'agg, 'com, 'evt>

    /// <summary>执行命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="ct">取消凭据。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="sub">EventStore持久订阅客户端。</param>
    /// <param name="group">订阅组。</param>
    /// <param name="dic">命令代理字典。</param>
    /// <returns>订阅并执行命令的后台任务</returns>
    val inline run<'agg> :
        ct: CancellationToken ->
        logger: ILogger ->
        sub: ISubscriber ->
        group: string ->
        dic: IDictionary<string, MailboxProcessor<ResolvedEvent>> ->
            Tasks.Task
            when 'agg :> Aggregate
