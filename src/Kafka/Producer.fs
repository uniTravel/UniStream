namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IProducer<'k, 'v> =

    abstract member Client: Confluent.Kafka.IProducer<'k, 'v>


[<Sealed>]
type AggregateProducer(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer<string, byte array> with
        member _.Client =
            let cfg = options.Get(Cons.Agg)
            ProducerBuilder<string, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer<string, byte array>).Client.Dispose()
                dispose <- true


[<Sealed>]
type CommandProducer(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer<string, byte array> with
        member _.Client =
            let cfg = options.Get(Cons.Com)
            ProducerBuilder<string, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer<string, byte array>).Client.Dispose()
                dispose <- true
