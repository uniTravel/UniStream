namespace UniStream.Domain

open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection


/// <summary>聚合投影者类型
/// </summary>
type AggProjector<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="producer">Kafka聚合生产者。</param>
    /// <param name="consumer">Kafka聚合消费者。</param>
    new:
        logger: ILogger<AggProjector<'agg>> *
        [<FromKeyedServices(Cons.Agg)>] producer: IProducer<string, byte array> *
        [<FromKeyedServices(Cons.Agg)>] consumer: IConsumer<string, byte array> ->
            AggProjector<'agg>

    interface IWorker
