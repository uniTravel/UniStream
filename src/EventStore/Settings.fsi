namespace UniStream.Domain

open Microsoft.Extensions.Options
open EventStore.Client


/// <summary>EventStore客户端设置接口
/// </summary>
[<Interface>]
type ISettings =

    /// <summary>EventStore客户端设置
    /// </summary>
    abstract member Settings: EventStoreClientSettings


/// <summary>EventStore客户端设置
/// </summary>
[<Sealed>]
type Settings =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">EventStore配置选项。</param>
    new: options: IOptions<EventStoreOptions> -> Settings

    interface ISettings
