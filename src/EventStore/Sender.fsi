namespace UniStream.Domain

open System
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open EventStore.Client


/// <summary>聚合命令发送者消息类型
/// </summary>
type Msg =
    | Send of Guid * Uuid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of EventRecord
    | Refresh of DateTime


/// <summary>聚合命令发送者接口
/// </summary>
[<Interface>]
type ISender =

    /// <summary>聚合命令发送代理
    /// </summary>
    abstract member Agent: MailboxProcessor<Msg>


/// <summary>聚合命令发送者类型
/// </summary>
[<Sealed>]
type Sender<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="cfg">EventStore配置选项。</param>
    /// <param name="options">命令配置选项。</param>
    /// <param name="sub">EventStore持久订阅客户端。</param>
    /// <param name="client">EventStore客户端。</param>
    new:
        logger: ILogger<Sender<'agg>> *
        cfg: IOptions<EventStoreOptions> *
        options: IOptionsMonitor<CommandOptions> *
        sub: IPersistent *
        client: IClient ->
            Sender<'agg>

    interface ISender

    interface IDisposable


/// <summary>聚合命令发送者模块
/// </summary>
[<RequireQualifiedAccess>]
module Sender =

    /// <summary>发送命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="sender">聚合命令发送者。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    val inline send<'agg, 'com, 'evt> :
        sender: ISender -> aggId: Guid -> comId: Guid -> com: 'com -> Async<unit> when Com<'agg, 'com, 'evt>
