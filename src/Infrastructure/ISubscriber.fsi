namespace UniStream.Domain

open System


/// <summary>聚合命令订阅者接口
/// </summary>
[<Interface>]
type ISubscriber<'agg when 'agg :> Aggregate> =
    inherit IWorker<'agg>

    /// <summary>添加聚合命令处理者
    /// </summary>
    /// <param name="key">命令类型全称。</param>
    /// <param name="hangler">聚合命令处理者。</param>
    abstract member AddHandler: key: string -> handler: MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>> -> unit
