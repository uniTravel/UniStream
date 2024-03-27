namespace UniStream.Domain

open EventStore.Client


/// <summary>EventStore持久订阅客户端接口
/// </summary>
[<Interface>]
type ISubscriber =

    /// <summary>EventStore持久订阅客户端
    /// </summary>
    abstract member Subscriber: EventStorePersistentSubscriptionsClient


/// <summary>EventStore持久订阅客户端
/// </summary>
[<Sealed>]
type Subscriber =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="settings">EventStore客户端设置。</param>
    new: settings: ISettings -> Subscriber

    interface ISubscriber
