namespace UniStream.Domain

open System
open EventStore.Client


/// <summary>EventStore投影管理客户端接口
/// </summary>
[<Interface>]
type IManager =

    /// <summary>EventStore投影管理客户端
    /// </summary>
    abstract member Manager: EventStoreProjectionManagementClient


/// <summary>EventStore投影管理客户端
/// </summary>
[<Sealed>]
type Manager =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="settings">EventStore客户端设置。</param>
    new: settings: ISettings -> Manager

    interface IManager

    interface IDisposable
