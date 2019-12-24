module EventStore.Tests

open System
open System.Text
open Microsoft.FSharp.Core.Operators
open Expecto
open EventStore.ClientAPI

let uri = Uri "tcp://admin:changeit@localhost:1113"
let conn = Array.zeroCreate 5
[ 0 .. 4 ] |> List.iter (fun i ->
    conn.[i] <- EventStoreConnection.Create uri
    conn.[i].ConnectAsync () |> Async.AwaitTask |> Async.RunSynchronously |> ignore
)

[<PTests>]
let tests =
    testSequenced <| testList "EventStore" [
        let withArgs f () =
            let streamName = "newStream"
            go "EventStore" |> f streamName
        yield! testFixture withArgs [
            "写入流，一个连接", fun streamName finish ->
                let ds =
                    seq { 1 .. 50000 }
                    |> Seq.map (fun i ->
                        let meta = sprintf "%d" i
                        EventData (Guid.NewGuid (), "te", false, Encoding.UTF8.GetBytes "some data", Encoding.UTF8.GetBytes meta)
                    )
                conn.[0].AppendToStreamAsync (streamName, int64 ExpectedVersion.Any, ds) |> Async.AwaitTask
                |> Async.RunSynchronously
                |> ignore
                finish 1
            "写入流，五个连接并发", fun streamName finish ->
                [ 0 .. 4 ]
                |> List.map (fun j ->
                    let ds =
                        seq { 1 .. 10000 }
                        |> Seq.map (fun i ->
                            let meta = sprintf "%d" (j * 10 + i)
                            EventData (Guid.NewGuid (), "te", false, Encoding.UTF8.GetBytes "some data", Encoding.UTF8.GetBytes meta)
                        )
                    conn.[j].AppendToStreamAsync (streamName, int64 ExpectedVersion.Any, ds) |> Async.AwaitTask
                )
                |> Async.Parallel
                |> Async.RunSynchronously
                |> ignore
                finish 2
            "顺序读", fun streamName finish ->
                let rs = (conn.[0].ReadStreamEventsForwardAsync(streamName, int64 StreamPosition.Start, 4 * 1024, true)).Result
                finish 3
            "反向读", fun streamName finish ->
                let rs = (conn.[0].ReadStreamEventsBackwardAsync(streamName, int64 StreamPosition.End, 4 * 1024, true)).Result
                finish 4
        ]
    ]
    |> testLabel "UniStream.Domain"