namespace UniStream.Domain

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options
open Confluent.Kafka


[<Interface>]
type IConsumer<'agg when 'agg :> Aggregate> =

    abstract member Client: IConsumer<byte array, byte array>


[<Sealed>]
type AggConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get(Cons.Agg + typeof<'agg>.Name)
            cfg.GroupId <- typeof<'agg>.FullName
            cfg.AutoOffsetReset <- AutoOffsetReset.Earliest
            cfg.EnableAutoCommit <- false
            cfg.IsolationLevel <- IsolationLevel.ReadCommitted
            cfg.EnablePartitionEof <- true
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Close()
                dispose <- true


[<Sealed>]
type ComConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>, config: IConfiguration) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get(Cons.Com + typeof<'agg>.Name)
            let aggType = typeof<'agg>.FullName
            let hostname = config["Kafka:Hostname"]
            cfg.GroupId <- aggType + "-Command"
            cfg.GroupInstanceId <- aggType + "-" + hostname
            cfg.PartitionAssignmentStrategy <- PartitionAssignmentStrategy.CooperativeSticky
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Close()
                dispose <- true


[<Sealed>]
type ReceiveConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>, config: IConfiguration) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get(Cons.Typ + typeof<'agg>.Name)
            let hostname = config["Kafka:Hostname"]
            cfg.GroupId <- typeof<'agg>.FullName + "-" + hostname
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Close()
                dispose <- true


[<Sealed>]
type RestoreConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get(Cons.Typ + typeof<'agg>.Name)
            cfg.GroupId <- typeof<'agg>.FullName + "-Restore"
            cfg.AutoOffsetReset <- AutoOffsetReset.Earliest
            cfg.EnableAutoCommit <- false
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Close()
                dispose <- true


[<Sealed>]
type ProjectConsumer<'agg when 'agg :> Aggregate>(options: IOptionsMonitor<ConsumerConfig>) =
    let mutable dispose = false

    interface IConsumer<'agg> with
        member _.Client =
            let cfg = options.Get(Cons.Typ + typeof<'agg>.Name)
            cfg.GroupId <- typeof<'agg>.FullName + "-Projector"
            cfg.AutoOffsetReset <- AutoOffsetReset.Earliest
            cfg.EnableAutoCommit <- false
            cfg.IsolationLevel <- IsolationLevel.ReadCommitted
            ConsumerBuilder<byte array, byte array>(cfg).Build()

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IConsumer<'agg>).Client.Close()
                dispose <- true
