namespace UniStream.Domain

open System


/// <summary>聚合命令发送者接口
/// </summary>
[<Interface>]
type ISender<'agg when 'agg :> Aggregate> =

    /// <summary>聚合命令发送函数
    /// </summary>
    abstract member send: (Guid -> Guid -> string -> byte array -> Async<Result<unit, exn>>)
