namespace UniStream.Domain

open System
open EventStore.Client


type IPersistent =

    abstract member Subscriber: EventStorePersistentSubscriptionsClient


[<Sealed>]
type Persistent(settings: ISettings) =
    let mutable dispose = false

    interface IPersistent with
        member _.Subscriber = new EventStorePersistentSubscriptionsClient(settings.Settings)

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IPersistent).Subscriber.Dispose()
                dispose <- true
