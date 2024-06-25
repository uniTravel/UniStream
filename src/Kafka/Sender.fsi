namespace UniStream.Domain

open System
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open UniStream.Domain


/// <summary>聚合命令发送者类型
/// </summary>
[<Sealed>]
type Sender<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="options">命令配置选项。</param>
    /// <param name="producer">Kafka命令生产者。</param>
    /// <param name="consumer">Kafka命令消费者。</param>
    new:
        logger: ILogger<Sender<'agg>> *
        options: IOptionsMonitor<CommandOptions> *
        [<FromKeyedServices(Cons.Com)>] producer: IProducer *
        [<FromKeyedServices(Cons.Com)>] consumer: IConsumer ->
            Sender<'agg>

    interface ISender<'agg>

    interface IDisposable
