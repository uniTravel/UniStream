namespace UniStream.Domain

open System


type Msg =
    | Send of Guid * Guid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of Guid * (Guid -> unit)
    | Refresh of DateTime


type ISender<'agg when 'agg :> Aggregate> =

    abstract member Agent: MailboxProcessor<Msg>
