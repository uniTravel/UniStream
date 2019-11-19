module EventStore.Tests

open System
open System.Text
open Microsoft.FSharp.Core.Operators
open Expecto
open EventStore.ClientAPI

[<PTests>]
let tests =
    testList "EventStore" [
        let withArgs f () =
            let uri = Uri "tcp://admin:changeit@localhost:1113"
            let streamName = "newStream"
            let conn = EventStoreConnection.Create uri
            conn.ConnectAsync () |> Async.AwaitTask |> Async.RunSynchronously |> ignore
            go "EventStore" |> f conn streamName
            conn.Close ()
        yield! testFixture withArgs [
            "写入流", fun conn streamName finish ->
                let evn = EventData (Guid.NewGuid (), "testEvent", false, Encoding.UTF8.GetBytes "some data", Encoding.UTF8.GetBytes "some metadata")
                let writeResult = conn.AppendToStreamAsync (streamName, int64 ExpectedVersion.Any, evn) |> Async.AwaitTask |> Async.RunSynchronously
                let rs = (conn.ReadStreamEventsForwardAsync(streamName, 0L, 10, true)).Result
                let r = (conn.ReadEventAsync(streamName, 0L, true)).Result.Event.Value.Event
                finish 1
        ]
    ]