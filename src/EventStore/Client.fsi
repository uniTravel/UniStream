namespace UniStream.Domain

open EventStore.Client


/// <summary>EventStore客户端接口
/// </summary>
[<Interface>]
type IClient =

    /// <summary>EventStore客户端
    /// </summary>
    abstract member Client: EventStoreClient


/// <summary>EventStore客户端
/// </summary>
[<Sealed>]
type Client =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="settings">EventStore客户端设置。</param>
    new: settings: ISettings -> Client

    interface IClient
