module Note.Tests

open System
open Expecto
open Note.Domain

let aggId = Guid.NewGuid()

[<Tests>]
let tests =
    testSequenced <| testList "NoteAgg" [
        testCase "Create Note" <| fun _ ->
            let traceId = Guid.NewGuid()
            let command = CreateNote.create { Title = "title"; Content = "first content" }
            Async.RunSynchronously <| applyCommand note aggId traceId command
        testCase "Change Note" <| fun _ ->
            let traceId = Guid.NewGuid()
            let command = ChangeNote.create { Content = "changed content" }
            Async.RunSynchronously <| applyCommand note aggId traceId command
    ]
    |> testLabel "Note App"