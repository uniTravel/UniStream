namespace UniStream.Domain

open Microsoft.Extensions.Logging


/// <summary>聚合命令订阅者类型
/// </summary>
[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="sub">EventStore持久订阅客户端。</param>
    new: logger: ILogger<Subscriber<'agg>> * sub: IPersistent -> Subscriber<'agg>

    interface ISubscriber<'agg>
