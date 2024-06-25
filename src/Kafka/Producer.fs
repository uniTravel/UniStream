namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IProducer =

    abstract member Client: IProducer<byte array, byte array>


[<Sealed>]
type AggregateProducer(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer with
        member _.Client =
            let cfg = options.Get(Cons.Agg)
            ProducerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer).Client.Dispose()
                dispose <- true


[<Sealed>]
type CommandProducer(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer with
        member _.Client =
            let cfg = options.Get(Cons.Com)
            ProducerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer).Client.Dispose()
                dispose <- true
