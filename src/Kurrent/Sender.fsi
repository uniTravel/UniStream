namespace UniStream.Domain

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options


/// <summary>聚合命令发送者类型
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
[<Sealed>]
type Sender<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="config">应用配置。</param>
    /// <param name="options">命令配置选项。</param>
    /// <param name="sub">Kurrent持久订阅客户端。</param>
    /// <param name="client">Kurrent客户端。</param>
    new:
        logger: ILogger<Sender<'agg>> *
        config: IConfiguration *
        options: IOptionsMonitor<CommandOptions> *
        sub: IPersistent *
        client: IClient ->
            Sender<'agg>

    interface ISender<'agg>

    interface IDisposable
