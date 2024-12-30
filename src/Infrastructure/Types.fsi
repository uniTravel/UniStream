namespace UniStream.Domain

open System


/// <summary>聚合命令发送者消息类型
/// </summary>
type internal Msg =
    | Send of Guid * Guid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of Guid * Result<unit, exn>
    | Refresh of DateTime


/// <summary>聚合命令执行结果
/// </summary>
type ComResult =
    | Success
    | Duplicate
    | Fail of exn
