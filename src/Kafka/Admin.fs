namespace UniStream.Domain

open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IAdmin =

    abstract member Client: IAdminClient


[<Sealed>]
type Admin(options: IOptions<AdminClientConfig>) =
    let cfg = options.Value

    interface IAdmin with
        member _.Client = AdminClientBuilder(cfg).Build()
