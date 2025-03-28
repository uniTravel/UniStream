namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IAdmin<'agg when 'agg :> Aggregate> =

    abstract member Client: IAdminClient


[<Sealed>]
type Admin<'agg when 'agg :> Aggregate>(options: IOptions<AdminClientConfig>) =
    let cfg = options.Value
    let mutable dispose = false

    interface IAdmin<'agg> with
        member _.Client = AdminClientBuilder(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IAdmin<'agg>).Client.Dispose()
                dispose <- true
