namespace UniStream.Domain

open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection


/// <summary>命令投影者类型
/// </summary>
type ComProjector<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="producer">Kafka聚合生产者。</param>
    /// <param name="consumer">Kafka聚合消费者。</param>
    new:
        logger: ILogger<ComProjector<'agg>> *
        [<FromKeyedServices(Cons.Agg)>] producer: IProducer<string, byte array> *
        [<FromKeyedServices(Cons.Agg)>] consumer: IConsumer<string, byte array> ->
            ComProjector<'agg>

    interface IWorker
