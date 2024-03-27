namespace UniStream.Domain

open EventStore.Client


type IClient =

    abstract member Client: EventStoreClient


[<Sealed>]
type Client(settings: ISettings) =
    interface IClient with
        member _.Client = new EventStoreClient(settings.Settings)
