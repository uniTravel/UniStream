namespace UniStream.Domain

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Confluent.Kafka
open UniStream.Domain


/// <summary>Stream类型
/// </summary>
[<Sealed>]
type Stream<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="options">Kafka消费者配置选项。</param>
    /// <param name="admin">Kafka管理者配置。</param>
    /// <param name="producer">Kafka聚合生产者。</param>
    /// <param name="consumer">Kafka聚合消费者。</param>
    new:
        logger: ILogger<Stream<'agg>> *
        options: IOptionsMonitor<ConsumerConfig> *
        admin: IAdmin *
        [<FromKeyedServices(Cons.Agg)>] producer: IProducer *
        [<FromKeyedServices(Cons.Agg)>] consumer: IConsumer ->
            Stream<'agg>

    interface IStream<'agg>
