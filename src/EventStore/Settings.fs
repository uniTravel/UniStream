namespace UniStream.Domain

open Microsoft.Extensions.Options
open EventStore.Client


type ISettings =

    abstract member Settings: EventStoreClientSettings


[<Sealed>]
type Settings(options: IOptions<EventStoreOptions>) =
    let cfg = options.Value
    let conn = $"esdb://{cfg.User}:{cfg.Pass}@{cfg.Host}?tlsVerifyCert={cfg.VerifyCert}"

    interface ISettings with
        member _.Settings = EventStoreClientSettings.Create conn
