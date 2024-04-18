namespace UniStream.Domain

open System
open System.Threading
open Microsoft.Extensions.Logging


/// <summary>命令处理后台模块
/// </summary>
[<RequireQualifiedAccess>]
module Worker =

    /// <summary>发送命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    val inline send<'agg, 'com, 'evt> :
        client: IClient -> aggId: Guid -> com: 'com -> Async<unit> when Com<'agg, 'com, 'evt>

    /// <summary>执行命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="ct">取消凭据。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="sub">EventStore持久订阅客户端。</param>
    /// <param name="group">订阅组。</param>
    /// <param name="f">处理流数据的回调函数。</param>
    /// <returns>订阅并执行命令的后台任务</returns>
    val inline run<'agg, 'com, 'evt> :
        ct: CancellationToken ->
        logger: ILogger ->
        client: IClient ->
        sub: ISubscriber ->
        group: string ->
        f: (Guid option -> Guid -> 'com -> Async<'agg>) ->
            Tasks.Task
            when Com<'agg, 'com, 'evt>
