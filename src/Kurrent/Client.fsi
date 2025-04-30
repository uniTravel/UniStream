namespace UniStream.Domain

open System
open EventStore.Client


/// <summary>Kurrent客户端接口
/// </summary>
[<Interface>]
type IClient =

    /// <summary>Kurrent客户端
    /// </summary>
    abstract member Client: EventStoreClient


/// <summary>Kurrent客户端
/// </summary>
[<Sealed>]
type Client =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="settings">Kurrent客户端设置。</param>
    new: settings: ISettings -> Client

    interface IClient

    interface IDisposable
