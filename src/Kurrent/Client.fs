namespace UniStream.Domain

open System
open EventStore.Client


type IClient =

    abstract member Client: EventStoreClient


[<Sealed>]
type Client(settings: ISettings) =
    let mutable dispose = false

    interface IClient with
        member _.Client = new EventStoreClient(settings.Settings)

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IClient).Client.Dispose()
                dispose <- true
