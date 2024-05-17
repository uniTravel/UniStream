namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IConsumer<'k, 'v> =

    abstract member Client: Confluent.Kafka.IConsumer<'k, 'v>


[<Sealed>]
type AggregateConsumer(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<string, byte array> with
        member _.Client =
            let cfg = options.Get(Cons.Agg)
            ConsumerBuilder<string, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<string, byte array>).Client.Dispose()
                dispose <- true


[<Sealed>]
type CommandConsumer(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<string, byte array> with
        member _.Client =
            let cfg = options.Get(Cons.Com)
            ConsumerBuilder<string, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<string, byte array>).Client.Dispose()
                dispose <- true
