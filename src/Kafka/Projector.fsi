namespace UniStream.Domain

open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging


/// <summary>聚合投影者类型
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
[<Sealed>]
type Projector<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="ap">Kafka聚合生产者。</param>
    /// <param name="tc">Kafka聚合类型消费者。</param>
    new: logger: ILogger<Projector<'agg>> * ap: IProducer<'agg> * tc: IConsumer<'agg> -> Projector<'agg>

    inherit BackgroundService
