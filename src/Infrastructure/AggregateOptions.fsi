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

    /// <summary>聚合缓存容量，缺省为10000
    /// </summary>
    [<Range(2, Int32.MaxValue >>> 3)>]
    member Capacity: int with get, set

    /// <summary>操作容量倍数，缺省为3
    /// </summary>
    /// <remarks>
    /// <para>1、用于计算操作容量。</para>
    /// <para>2、操作容量系触发缓存刷新的操作计数。</para>
    /// </remarks>
    [<Range(2, 7)>]
    member Multiple: int with get, set

    /// <summary>初始化缓存最近的命令操作，缺省为30
    /// </summary>
    /// <remarks>
    /// <para>1、单位为分钟。</para>
    /// <para>2、用于命令去重。</para>
    /// <para>3、需要权衡请求超时、期间请求数加以设置。</para>
    /// </remarks>
    [<Range(2, Int32.MaxValue >>> 3)>]
    member Latest: int with get, set
