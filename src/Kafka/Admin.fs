namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IAdmin =

    abstract member Client: IAdminClient


[<Sealed>]
type Admin(options: IOptions<AdminClientConfig>) =
    let cfg = options.Value
    let mutable dispose = false

    interface IAdmin with
        member _.Client = AdminClientBuilder(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IAdmin).Client.Dispose()
                dispose <- true
