namespace UniStream.Domain

open System


type Msg =
    | Send of Guid * Guid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of Guid * Result<unit, exn>
    | Refresh of DateTime


type ComResult =
    | Success
    | Duplicate
    | Fail of exn
