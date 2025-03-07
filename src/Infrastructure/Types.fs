namespace UniStream.Domain

open System
open System.ComponentModel.DataAnnotations


type Msg =
    | Send of Guid * Guid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of Guid * Result<unit, exn>
    | Refresh of DateTime


type ComResult =
    | Success
    | Duplicate
    | Fail of exn


[<Sealed>]
[<AttributeUsage(AttributeTargets.Property)>]
type ValidGuidAttribute() =
    inherit ValidationAttribute(ErrorMessage = "无效的Guid格式或值为空")

    override _.IsValid(value: obj) =
        match value with
        | :? Guid as guid -> guid <> Guid.Empty
        | :? string as s ->
            match Guid.TryParse(s) with
            | true, parsedGuid -> parsedGuid <> Guid.Empty
            | _ -> false
        | _ -> false
