module Domain.Tests

open Expecto


[<FTests>]
let tests =
    testList "Domain" [
        testCase "MetaLog" <| fun _ ->
            printfn "Done!"
    ]
    |> testLabel "UniStream.Domain"