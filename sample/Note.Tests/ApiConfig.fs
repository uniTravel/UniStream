[<AutoOpen>]
module ApiConfig

open System
open EventStore.ClientAPI
open UniStream.Infrastructure
open Note.Application


let connect (uri: Uri) =
    let conn = EventStoreConnection.Create uri
    conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
    conn

let esUri = Uri "tcp://admin:changeit@localhost:4011"
let ldUri = Uri "tcp://admin:changeit@localhost:4012"
let lgUri = Uri "tcp://admin:changeit@localhost:4013"

let c1 = connect esUri
let c2 = connect ldUri
let c3 = connect lgUri

let reader = DomainEvent.get c1
let writer = DomainEvent.write c1
let ld = DomainLog.write c2
let lg = DiagnoseLog.write c3
let sub = DomainEvent.subscribe c1

let app = AppService (reader, writer, ld, lg)
app.AddNoteObserver sub