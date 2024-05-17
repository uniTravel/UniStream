namespace UniStream.Domain

open System
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Confluent.Kafka
open UniStream.Domain


/// <summary>聚合命令发送者类型
/// </summary>
type Sender<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="producer">Kafka命令生产者。</param>
    /// <param name="consumer">Kafka命令消费者。</param>
    new:
        logger: ILogger<Sender<'agg>> *
        [<FromKeyedServices(Cons.Com)>] producer: IProducer<Guid, byte array> *
        [<FromKeyedServices(Cons.Com)>] consumer: IConsumer<Guid, byte array> ->
            Sender<'agg>

    /// <summary>聚合命令接收主题相应分区
    /// </summary>
    member Partition: int

    /// <summary>聚合命令发送代理
    /// </summary>
    member Agent: MailboxProcessor<Guid * Message<Guid, byte array> * AsyncReplyChannel<Result<unit, exn>>>

    interface IDisposable


/// <summary>聚合命令发送者模块
/// </summary>
[<RequireQualifiedAccess>]
module Sender =

    /// <summary>发送命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="sender">聚合命令发送者。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="com">命令。</param>
    val inline send<'agg, 'com, 'evt> :
        sender: Sender<'agg> -> aggId: Guid -> com: 'com -> Async<unit> when Com<'agg, 'com, 'evt>
