module Domain.Tests.Logging

open Expecto


[<PTests>]
let tests =
    testSequenced <| testList "Logging" [
        testCase "" <| fun _ ->
            let q = 1
            printfn "Done"
    ] |> testLabel "Domain"