namespace UniStream.Domain

open System
open EventStore.Client


type ISubscriber =

    abstract member Subscriber: EventStorePersistentSubscriptionsClient


[<Sealed>]
type Subscriber(settings: ISettings) =
    let mutable dispose = false

    interface ISubscriber with
        member _.Subscriber = new EventStorePersistentSubscriptionsClient(settings.Settings)

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> ISubscriber).Subscriber.Dispose()
                dispose <- true
