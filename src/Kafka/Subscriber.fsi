namespace UniStream.Domain

open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open UniStream.Domain


/// <summary>聚合命令订阅者接口
/// </summary>
[<Interface>]
type ISubscriber =
    inherit IWorker

    /// <summary>添加聚合命令处理者
    /// </summary>
    /// <param name="key">命令类型全称。</param>
    /// <param name="hangler">聚合命令处理者。</param>
    abstract member AddHandler: key: string -> handler: MailboxProcessor<string * string * byte array> -> unit


/// <summary>聚合命令订阅者类型
/// </summary>
[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="consumer">Kafka命令消费者。</param>
    new:
        logger: ILogger<Subscriber<'agg>> * [<FromKeyedServices(Cons.Com)>] consumer: IConsumer<string, byte array> ->
            Subscriber<'agg>

    interface ISubscriber
