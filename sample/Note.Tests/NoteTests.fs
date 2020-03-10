module Note.Tests

open System
open Expecto
open Note.Domain


[<Tests>]
let tests =
    let aggId = Guid.NewGuid().ToString()
    testSequenced <| testList "NoteAgg" [
        let withArgs f () =
            go "NoteAgg" |> f
        yield! testFixture withArgs [
            "创建Note", fun finish ->
                let traceId = Guid.NewGuid().ToString()
                let command : CreateNote = { Title = "title"; Content = "initial content" }
                let reply = app.CreateNote aggId traceId command |> Async.RunSynchronously
                Expect.equal reply.Title command.Title "返回值错误。"
                Expect.equal reply.Content command.Content "返回值错误。"
                finish 1
            "重复创建Note", fun finish ->
                let traceId = Guid.NewGuid().ToString()
                let command : CreateNote = { Title = "title"; Content = "initial content" }
                let f = fun _ -> app.CreateNote aggId traceId command |> Async.RunSynchronously |> ignore
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 2
            "修改Note", fun finish ->
                let traceId = Guid.NewGuid().ToString()
                let command : ChangeNote = {Content = "changed content" }
                let reply = app.ChangeNote aggId traceId command |> Async.RunSynchronously
                Expect.equal reply.Title "title" "返回值错误。"
                Expect.equal reply.Content command.Content "返回值错误。"
                finish 3
        ]
    ]
    |> testLabel "Note App"