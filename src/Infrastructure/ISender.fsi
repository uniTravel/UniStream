namespace UniStream.Domain

open System


/// <summary>聚合命令发送者消息类型
/// </summary>
type Msg =
    | Send of Guid * Guid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of Guid * (Guid -> unit)
    | Refresh of DateTime


/// <summary>聚合命令发送者接口
/// </summary>
[<Interface>]
type ISender<'agg when 'agg :> Aggregate> =

    /// <summary>聚合命令发送代理
    /// </summary>
    abstract member Agent: MailboxProcessor<Msg>
