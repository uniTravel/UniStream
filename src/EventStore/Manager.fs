namespace UniStream.Domain

open System
open EventStore.Client


type IManager =

    abstract member Manager: EventStoreProjectionManagementClient


[<Sealed>]
type Manager(settings: ISettings) =
    let mutable dispose = false

    interface IManager with
        member _.Manager = new EventStoreProjectionManagementClient(settings.Settings)

    interface IDisposable with
        member me.Dispose() =
            if not dispose then
                (me :> IManager).Manager.Dispose()
                dispose <- true
