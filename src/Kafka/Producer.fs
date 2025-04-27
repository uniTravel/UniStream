namespace UniStream.Domain

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IProducer<'agg when 'agg :> Aggregate> =

    abstract member Client: IProducer<byte array, byte array>


[<Sealed>]
type AggProducer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ProducerConfig>, config: IConfiguration) =
    let mutable dispose = false

    interface IProducer<'agg> with
        member _.Client =
            let cfg = options.Get(Cons.Agg + typeof<'agg>.Name)
            let aggType = typeof<'agg>.FullName
            let hostname = config["Kafka:Hostname"]
            cfg.TransactionalId <- aggType + "-" + hostname
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
            let cfg = options.Get(Cons.Com + typeof<'agg>.Name)
            ProducerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer<'agg>).Client.Dispose()
                dispose <- true


[<Sealed>]
type TypProducer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ProducerConfig>) =
    let mutable dispose = false

    interface IProducer<'agg> with
        member _.Client =
            let cfg = options.Get(Cons.Typ + typeof<'agg>.Name)
            ProducerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IProducer<'agg>).Client.Dispose()
                dispose <- true
