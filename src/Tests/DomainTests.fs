module Domain.Tests

open Expecto


[<PTests>]
let tests =
    testList "Domain" [
        testCase "MetaLog" <| fun _ ->
            printfn "Done!"
    ]
    |> testLabel "UniStream.Domain"