namespace UniStream.Domain

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Confluent.Kafka
open UniStream.Domain


/// <summary>Stream类型
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
[<Sealed>]
type Stream<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="options">Kafka聚合消费者配置选项。</param>
    /// <param name="admin">Kafka管理者配置。</param>
    /// <param name="tp">Kafka聚合类型生产者。</param>
    /// <param name="tc">Kafka聚合类型消费者。</param>
    new:
        logger: ILogger<Stream<'agg>> *
        options: IOptionsMonitor<ConsumerConfig> *
        admin: IAdmin *
        [<FromKeyedServices(Cons.Typ)>] tp: IProducer *
        [<FromKeyedServices(Cons.Typ)>] tc: IConsumer ->
            Stream<'agg>

    interface IStream<'agg>
