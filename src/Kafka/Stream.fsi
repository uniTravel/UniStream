namespace UniStream.Domain

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Options
open Confluent.Kafka
open UniStream.Domain


/// <summary>Stream类型
/// </summary>
[<Sealed>]
type Stream<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    /// <param name="admin">Kafka管理者配置。</param>
    /// <param name="producer">Kafka聚合生产者。</param>
    /// <param name="consumer">Kafka聚合消费者。</param>
    new:
        options: IOptionsMonitor<ConsumerConfig> *
        admin: IAdmin *
        [<FromKeyedServices(Cons.Agg)>] producer: IProducer<string, byte array> *
        [<FromKeyedServices(Cons.Agg)>] consumer: IConsumer<string, byte array> ->
            Stream<'agg>

    interface IStream
