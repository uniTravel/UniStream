namespace UniStream.Domain

open System
open System.ComponentModel.DataAnnotations


/// <summary>命令配置选项
/// </summary>
[<Sealed>]
type CommandOptions =

    /// <summary>主构造函数
    /// </summary>
    new: unit -> CommandOptions

    /// <summary>命令处理结果的刷新间隔，缺省为15
    /// </summary>
    /// <remarks>
    /// <para>1、单位为秒。</para>
    /// <para>2、意在节点宕机、请求路由到新节点之后，能得到正确结果且不会重复。</para>
    /// <para>3、参考请求超时的设置值。</para>
    /// </remarks>
    [<Range(2, Int32.MaxValue >>> 3)>]
    member Interval: int with get, set
