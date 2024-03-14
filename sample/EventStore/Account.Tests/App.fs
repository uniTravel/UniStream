[<AutoOpen>]
module Account.Domain.App

open EventStore.Client
open UniStream.Infrastructure


let conn =
    "esdb://admin:changeit@127.0.0.1:2111,127.0.0.1:2112,127.0.0.1:2113?tls=true&tlsVerifyCert=false"

let client = new EventStoreClient(EventStoreClientSettings.Create(conn))
let es = Stream(client)
