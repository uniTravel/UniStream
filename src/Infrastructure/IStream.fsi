namespace UniStream.Domain

open System
open System.Collections.Generic


/// <summary>领域流配置接口
/// </summary>
[<Interface>]
type IStream<'agg when 'agg :> Aggregate> =

    /// <summary>聚合事件写入流的函数
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="revision">聚合版本。</param>
    /// <param name="evtType">事件类型。</param>
    /// <param name="evtData">事件数据。</param>
    abstract member Writer: (Guid -> Guid -> uint64 -> string -> byte array -> unit)

    /// <summary>读取聚合事件流的函数
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>聚合事件流</returns>
    abstract member Reader: (Guid -> (string * ReadOnlyMemory<byte>) list)

    /// <summary>恢复命令操作记录缓存的函数
    /// </summary>
    /// <param name="ch">命令操作记录缓存。</param>
    /// <param name="count">要恢复的数量。</param>
    /// <returns>命令操作记录列表</returns>
    abstract member Restore: (HashSet<Guid> -> int -> Guid list)
