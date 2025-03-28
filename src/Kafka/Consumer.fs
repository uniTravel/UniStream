namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IConsumer<'agg when 'agg :> Aggregate> =

    abstract member Client: IConsumer<byte array, byte array>


[<Sealed>]
type TypConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get Cons.Typ
            cfg.GroupId <- typeof<'agg>.FullName + "-" + cfg.GroupId
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Dispose()
                dispose <- true


[<Sealed>]
type AggConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get Cons.Agg
            cfg.GroupId <- typeof<'agg>.FullName + "-" + cfg.GroupId
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Dispose()
                dispose <- true


[<Sealed>]
type ComConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get Cons.Com
            cfg.GroupId <- typeof<'agg>.FullName + "-" + cfg.GroupId
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Dispose()
                dispose <- true
