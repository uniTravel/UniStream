module Domain.Tests

open Expecto


[<Tests>]
let tests =
    testList "Domain" [
        testCase "MetaLog" <| fun _ ->
            printfn "Done!"
    ]
    |> testLabel "UniStream.Domain"