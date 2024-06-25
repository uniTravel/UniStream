namespace UniStream.Domain

open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open UniStream.Domain


/// <summary>聚合命令订阅者类型
/// </summary>
[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="consumer">Kafka命令消费者。</param>
    new: logger: ILogger<Subscriber<'agg>> * [<FromKeyedServices(Cons.Com)>] consumer: IConsumer -> Subscriber<'agg>

    interface ISubscriber<'agg>
