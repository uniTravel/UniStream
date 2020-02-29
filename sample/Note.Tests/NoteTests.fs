module Note.Tests

open Expecto
open Note.Contract


[<Tests>]
let tests =
    testSequenced <| testList "NoteAgg" [
        testCase "Create & Change Note" <| fun _ ->
            let command = CreateNote()
            command.Title <- "title"
            command.Content <- "initial content"
            let reply = app.CreateNote command |> Async.AwaitTask |> Async.RunSynchronously
            printfn "%s=%s" reply.AggId reply.TraceId
            let command = ChangeNote()
            command.AggId <- reply.AggId
            command.Content <- "changed content"
            let reply = app.ChangeNote command |> Async.AwaitTask |> Async.RunSynchronously
            printfn "%s" reply.TraceId
    ]
    |> testLabel "Note App"