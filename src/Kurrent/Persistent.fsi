namespace UniStream.Domain

open System
open EventStore.Client


/// <summary>Kurrent持久订阅客户端接口
/// </summary>
[<Interface>]
type IPersistent =

    /// <summary>Kurrent持久订阅客户端
    /// </summary>
    abstract member Subscriber: EventStorePersistentSubscriptionsClient


/// <summary>Kurrent持久订阅客户端
/// </summary>
[<Sealed>]
type Persistent =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="settings">Kurrent客户端设置。</param>
    new: settings: ISettings -> Persistent

    interface IPersistent

    interface IDisposable
