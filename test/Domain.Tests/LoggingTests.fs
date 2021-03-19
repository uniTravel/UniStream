module Domain.Tests.Logging

open Microsoft.FSharp.Core.Operators
open Expecto
open Hopac
open Logary
open Logary.Message
open Logary.Targets
open Logary.Internals
open Logary.Configuration


// let lg = Log.create "Logary.Baseline"
// let ri = RuntimeInfo.create "tests" "localhost"
// let flush = Target.flush >> Job.Ignore

[<Tests>]
let tests =
    testSequenced <| testList "Logging" [
        testCase "" <| fun _ ->
            printfn "Done"
        // testCaseJob "1" <| job {
        //     // let conf = Elasticsearch.create Elasticsearch.empty "es"
        //     let conf = LiterateConsole.create LiterateConsole.empty "console"
        //     let ri = RuntimeInfo.create "tests" "localhost"
        //     let! target = Target.create ri conf
        //     let msg = eventInfo "test message"
        //     let! ack = Target.tryLog target msg
        //     do! ack |> function
        //     | Ok ack -> ack
        //     | Result.Error e -> failtestf "Failure placing in buffer %A" e
        //     do! flush target }
    ]
    |> testLabel "Domain"