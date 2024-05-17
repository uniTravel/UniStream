namespace UniStream.Domain

open Microsoft.Extensions.DependencyInjection
open UniStream.Domain


/// <summary>Stream类型
/// </summary>
[<Sealed>]
type Stream =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="admin">Kafka管理者配置。</param>
    /// <param name="producer">Kafka聚合生产者。</param>
    /// <param name="consumer">Kafka聚合消费者。</param>
    new:
        admin: IAdmin *
        [<FromKeyedServices(Cons.Agg)>] producer: IProducer<string, byte array> *
        [<FromKeyedServices(Cons.Agg)>] consumer: IConsumer<string, byte array> ->
            Stream

    interface IStream
