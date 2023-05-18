namespace UniStream.Domain

open System


/// <summary>聚合的基类
/// </summary>
[<AbstractClass>]
type Aggregate =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="id">聚合ID。</param>
    new: id: Guid -> Aggregate

    /// <summary>执行领域命令
    /// </summary>
    /// <param name="cmType">领域命令类型。</param>
    /// <param name="cm">领域命令。</param>
    /// <returns>领域事件类型*领域事件</returns>
    abstract member Apply: cmType: string -> cm: ReadOnlyMemory<byte> -> string * ReadOnlyMemory<byte>

    /// <summary>重播领域事件
    /// </summary>
    /// <param name="evType">领域事件类型。</param>
    /// <param name="ev">领域事件。</param>
    abstract member Replay: evType: string -> ev: ReadOnlyMemory<byte> -> unit

    /// <summary>聚合ID
    /// </summary>
    member Id: Guid

    /// <summary>聚合版本
    /// </summary>
    member Revision: uint64

    /// <summary>执行领域命令的函数
    /// </summary>
    member ApplyCommand: (string -> ReadOnlyMemory<byte> -> string * ReadOnlyMemory<byte>)

    /// <summary>重播领域事件的函数
    /// </summary>
    member ReplayEvent: (string -> ReadOnlyMemory<byte> -> unit)
