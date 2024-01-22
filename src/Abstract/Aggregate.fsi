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

    /// <summary>聚合ID
    /// </summary>
    member Id: Guid

    /// <summary>聚合版本
    /// </summary>
    member Revision: uint64

    /// <summary>单步增长聚合版本
    /// </summary>
    member Next: unit -> unit
