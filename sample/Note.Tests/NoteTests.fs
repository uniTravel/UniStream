module Note.Tests

open System
open Expecto
open Note.Domain.NoteAgg


[<Tests>]
let tests =
    testList "NoteAgg" [
        testCase "Create Note" <| fun _ ->
            let aggId = Guid.NewGuid()
            let cb = [||]
            Async.Start <| applyRaw aggId cb CreateNote.create
        testCase "Change Note" <| fun _ ->
            let aggId = Guid.NewGuid()
            let cb = [||]
            Async.Start <| applyRaw aggId cb ChangeNote.create
    ]
    |> testLabel "Note App"