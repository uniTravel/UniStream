namespace UniStream.Infrastructure

open System
open EventStore.ClientAPI


module Helper =

    let Connect (uri: Uri) =
        let conn = EventStoreConnection.Create uri
        conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
        conn


module DomainEvent =

    type T = Client of IEventStoreConnection

    let create uri = Helper.Connect uri |> Client

    let get (Client client) aggType (aggId: Guid) version =
        let streamName = sprintf "%s-%O" aggType aggId
        let rec read events pos =
            let slice = (client.ReadStreamEventsForwardAsync (streamName, pos, 64, false)).Result
            let events = Array.append events slice.Events
            if slice.IsEndOfStream then events, slice.LastEventNumber
            else read events slice.NextEventNumber
        let events, version = read [||] version
        events |> Array.map (fun e -> (e.Event.EventId, e.Event.EventType, e.Event.Data)), version

    let write (Client client) aggType (aggId: Guid) version traceId deltaType delta =
        let streamName = sprintf "%s-%O" aggType aggId
        let version = version - 1L
        let eventData = EventData (traceId, deltaType, true, delta, [||])
        client.AppendToStreamAsync (streamName, version, eventData) |> Async.AwaitTask |> Async.RunSynchronously |> ignore


module DomainLog =

    type T = Client of IEventStoreConnection

    let create uri = Helper.Connect uri |> Client

    let write (Client client) (aggType: string) traceId status dLog =
        let eventData = EventData (traceId, status, false, dLog, [||])
        client.AppendToStreamAsync (aggType, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.RunSynchronously |> ignore


module DiagnoseLog =

    type T =
        { Client: IEventStoreConnection
          StreamName: string }

    let create uri stream = { Client = Helper.Connect uri; StreamName = stream }

    let write { Client = client; StreamName = stream } aggType gLog =
        let eventData = EventData (Guid.NewGuid(), aggType, false, gLog, [||])
        client.AppendToStreamAsync (stream, int64 ExpectedVersion.Any, eventData) |> Async.AwaitTask |> Async.RunSynchronously |> ignore