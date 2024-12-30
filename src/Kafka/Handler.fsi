namespace UniStream.Domain

open System
open Microsoft.Extensions.Logging
open UniStream.Domain


/// <summary>聚合命令处理者模块
/// </summary>
[<RequireQualifiedAccess>]
module Handler =

    /// <summary>注册聚合命令代理
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="subscriber">聚合命令订阅者。</param>
    /// <param name="logger">日志记录器。</param>
    /// <param name="tp">Kafka聚合类型生产者。</param>
    /// <param name="commit">提交命令的函数。</param>
    val inline register<'agg, 'com, 'evt> :
        subscriber: ISubscriber<'agg> ->
        logger: ILogger ->
        tp: IProducer ->
        commit: (Guid -> Guid -> 'com -> Async<ComResult>) ->
            unit
            when Com<'agg, 'com, 'evt>
