namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IConsumer =

    abstract member Client: IConsumer<byte array, byte array>


[<Sealed>]
type AggregateConsumer(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer with
        member _.Client =
            let cfg = options.Get(Cons.Agg)
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer).Client.Dispose()
                dispose <- true


[<Sealed>]
type CommandConsumer(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer with
        member _.Client =
            let cfg = options.Get(Cons.Com)
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer).Client.Dispose()
                dispose <- true
