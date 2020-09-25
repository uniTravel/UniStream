module Domain.Tests.Logging

open Expecto
open Hopac
open Logary
open Logary.Message
open Logary.Targets
open Logary.Internals


let ri = RuntimeInfo.create "tests" "localhost"
let flush = Target.flush >> Job.Ignore

[<Tests>]
let tests =
    testSequenced <| testList "Logging" [
        // testCaseJob "1" <| job {
        //     let conf = Elasticsearch.create Elasticsearch.empty "es"
        //     let! target = Target.create ri conf
        //     let msg = eventInfo "test message"
        //     let! ack = Target.tryLog target msg
        //     do! ack |> function
        //     | Ok ack -> ack
        //     | Result.Error e -> failtestf "Failure placing in buffer %A" e
        //     do! flush target
        // }
    ]
    |> testLabel "Domain"