module Note.Tests

open System
open Expecto
open Note.Domain.NoteAgg


[<Tests>]
let tests =
    testList "NoteAgg" [
        testCase "Create Note" <| fun _ ->
            let aggId = Guid.NewGuid()
            let traceId = Guid.NewGuid()
            let name = typeof<NoteCreated>.FullName
            let cb = [||]
            Async.Start <| applyRaw note aggId traceId name cb CreateNote.create
        testCase "Change Note" <| fun _ ->
            let aggId = Guid.NewGuid()
            let traceId = Guid.NewGuid()
            let name = typeof<NoteChanged>.FullName
            let cb = [||]
            Async.Start <| applyRaw note aggId traceId name cb ChangeNote.create
    ]
    |> testLabel "Note App"