namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IProducer<'agg when 'agg :> Aggregate> =

    abstract member Client: IProducer<byte array, byte array>


[<Sealed>]
type TypProducer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer<'agg> with
        member _.Client =
            let cfg = options.Get Cons.Typ
            ProducerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer<'agg>).Client.Dispose()
                dispose <- true


[<Sealed>]
type AggProducer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer<'agg> with
        member _.Client =
            let cfg = options.Get Cons.Agg
            ProducerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer<'agg>).Client.Dispose()
                dispose <- true


[<Sealed>]
type ComProducer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer<'agg> with
        member _.Client =
            let cfg = options.Get Cons.Com
            ProducerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer<'agg>).Client.Dispose()
                dispose <- true
