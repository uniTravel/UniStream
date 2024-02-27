namespace UniStream.Infrastructure

open System
open EventStore.Client


/// <summary>Stream类型
/// </summary>
type Stream =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="settings">EventStore客户端设置。</param>
    new: settings: EventStoreClientSettings -> Stream

    /// <summary>聚合事件写入流的函数
    /// </summary>
    member Write: (Guid option -> string -> Guid -> uint64 -> string -> byte array -> unit)

    /// <summary>读取聚合事件流的函数
    /// </summary>
    member Read: (string -> Guid -> (string * byte array) list)

    /// <summary>关闭EventStore客户端
    /// </summary>
    member Close: unit -> unit
