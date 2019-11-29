module Note.Tests

open Expecto

[<Tests>]
let tests =
    testList "Note" [
        testCase "MetaLog" <| fun _ ->
            printfn "Done!"
    ]