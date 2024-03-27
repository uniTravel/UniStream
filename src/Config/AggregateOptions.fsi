namespace UniStream.Domain

open System
open System.ComponentModel.DataAnnotations


/// <summary>聚合配置选项
/// </summary>
[<Sealed>]
type AggregateOptions =

    /// <summary>主构造函数
    /// </summary>
    new: unit -> AggregateOptions

    /// <summary>聚合缓存容量
    /// </summary>
    [<Range(1, Int32.MaxValue >>> 3)>]
    member Capacity: int with get, set

    /// <summary>聚合缓存刷新间隔
    /// </summary>
    /// <remarks>单位为秒。</remarks>
    [<Range(0.1, 7200.0)>]
    member Refresh: float with get, set
