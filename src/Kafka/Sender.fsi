namespace UniStream.Domain

open System
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open UniStream.Domain


/// <summary>聚合命令发送者类型
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
[<Sealed>]
type Sender<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="options">命令配置选项。</param>
    /// <param name="cp">Kafka命令生产者。</param>
    /// <param name="tc">Kafka聚合类型消费者。</param>
    new:
        logger: ILogger<Sender<'agg>> *
        options: IOptionsMonitor<CommandOptions> *
        cp: IProducer<'agg> *
        tc: IConsumer<'agg> ->
            Sender<'agg>

    interface ISender<'agg>

    interface IDisposable
