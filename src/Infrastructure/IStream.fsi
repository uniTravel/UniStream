namespace UniStream.Domain

open System


/// <summary>领域流配置接口
/// </summary>
[<Interface>]
type IStream =

    /// <summary>聚合事件写入流的函数
    /// </summary>
    /// <param name="aggType">聚合类型全称。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="revision">聚合版本。</param>
    /// <param name="evtType">事件类型。</param>
    /// <param name="evtData">事件数据。</param>
    abstract member Writer: (string -> Guid -> Guid -> uint64 -> string -> byte array -> unit)

    /// <summary>读取聚合事件流的函数
    /// </summary>
    /// <param name="aggType">聚合类型全称。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <returns>聚合事件流</returns>
    abstract member Reader: (string -> Guid -> (string * ReadOnlyMemory<byte>) list)
