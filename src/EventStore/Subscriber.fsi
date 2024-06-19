namespace UniStream.Domain

open Microsoft.Extensions.Logging
open EventStore.Client


/// <summary>聚合命令订阅者接口
/// </summary>
[<Interface>]
type ISubscriber =
    inherit IWorker

    /// <summary>添加聚合命令处理者
    /// </summary>
    /// <param name="key">命令类型全称。</param>
    /// <param name="hangler">聚合命令处理者。</param>
    abstract member AddHandler: key: string -> handler: MailboxProcessor<Uuid * EventRecord> -> unit


/// <summary>聚合命令订阅者类型
/// </summary>
[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="sub">EventStore持久订阅客户端。</param>
    new: logger: ILogger<Subscriber<'agg>> * sub: IPersistent -> Subscriber<'agg>

    interface ISubscriber
