module Note.Tests

open System
open Expecto
open Note.Contract


[<Tests>]
let tests =
    let aggId = Guid.NewGuid()
    testSequenced <| testList "NoteAgg" [
        testCase "Create Note" <| fun _ ->
            let traceId = Guid.NewGuid()
            let command = CreateNote()
            command.Title <- "title"
            command.Content <- "initial content"
            let reply = app.CreateNote aggId traceId command |> Async.RunSynchronously
            Expect.equal reply.Title command.Title "返回值错误。"
            Expect.equal reply.Content command.Content "返回值错误。"
        testCase "Change Note" <| fun _ ->
            let traceId = Guid.NewGuid()
            let command = ChangeNote()
            command.AggId <- aggId.ToString()
            command.Content <- "changed content"
            let reply = app.ChangeNote traceId command |> Async.RunSynchronously
            Expect.equal reply.Title "title" "返回值错误。"
            Expect.equal reply.Content command.Content "返回值错误。"
    ]
    |> testLabel "Note App"