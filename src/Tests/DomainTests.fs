module Domain.Tests

open System.Collections.Generic
open Expecto


[<Tests>]
let tests =
    testList "Domain" [
        testCase "MetaLog" <| fun _ ->
            let queue = new Queue<int>()
            [ 1 .. 5 ]
            |> List.iter queue.Enqueue
            [ 1 .. 5 ]
            |> List.iter (fun _ ->
                let q = queue.Dequeue()
                ()
            )
            printfn "Done!"
    ]
    |> testLabel "UniStream.Domain"