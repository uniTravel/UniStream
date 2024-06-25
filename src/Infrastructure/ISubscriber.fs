namespace UniStream.Domain

open System


type ISubscriber<'agg when 'agg :> Aggregate> =
    inherit IWorker<'agg>

    abstract member AddHandler: key: string -> handler: MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>> -> unit
