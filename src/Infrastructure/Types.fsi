namespace UniStream.Domain

open System
open System.ComponentModel.DataAnnotations


/// <summary>聚合命令发送者消息类型
/// </summary>
type internal Msg =
    | Send of Guid * Guid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of Guid * Result<unit, exn>
    | Refresh of DateTime


/// <summary>聚合命令积压项消息类型
/// </summary>
type internal Backlog =
    | Add of Guid * AsyncReplyChannel<Result<unit, exn>>
    | Remove of Guid * Result<unit, exn>


/// <summary>聚合命令执行结果
/// </summary>
type ComResult =
    | Success
    | Duplicate
    | Fail of exn


/// <summary>验证Guid
/// </summary>
[<Sealed>]
[<AttributeUsage(AttributeTargets.Property)>]
type ValidGuidAttribute =
    inherit ValidationAttribute
    new: unit -> ValidGuidAttribute
