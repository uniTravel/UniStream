namespace UniStream.Domain

open Microsoft.Extensions.Options
open EventStore.Client


/// <summary>Kurrent客户端设置接口
/// </summary>
[<Interface>]
type ISettings =

    /// <summary>Kurrent客户端设置
    /// </summary>
    abstract member Settings: EventStoreClientSettings


/// <summary>Kurrent客户端设置
/// </summary>
[<Sealed>]
type Settings =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kurrent配置选项。</param>
    new: options: IOptions<KurrentOptions> -> Settings

    interface ISettings
