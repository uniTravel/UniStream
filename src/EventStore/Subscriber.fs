namespace UniStream.Domain

open EventStore.Client


type ISubscriber =

    abstract member Subscriber: EventStorePersistentSubscriptionsClient


[<Sealed>]
type Subscriber(settings: ISettings) =

    interface ISubscriber with
        member _.Subscriber = new EventStorePersistentSubscriptionsClient(settings.Settings)
