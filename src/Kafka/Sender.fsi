namespace UniStream.Domain

open System
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Confluent.Kafka
open UniStream.Domain


/// <summary>聚合命令发送者接口
/// </summary>
[<Interface>]
type ISender =

    /// <summary>聚合命令发送代理
    /// </summary>
    abstract member Agent: MailboxProcessor<string * Message<string, byte array> * AsyncReplyChannel<Result<unit, exn>>>

    /// <summary>聚合命令发送者分区
    /// </summary>
    abstract member Partition: int


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
        [<FromKeyedServices(Cons.Com)>] producer: IProducer<string, byte array> *
        [<FromKeyedServices(Cons.Com)>] consumer: IConsumer<string, byte array> ->
            Sender<'agg>

    interface ISender

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
        sender: ISender -> aggId: Guid -> com: 'com -> Async<unit> when Com<'agg, 'com, 'evt>
