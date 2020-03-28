module Note.Tests

open System
open Expecto
open Note.Domain


[<Tests>]
let tests =
    let aggId = Guid.NewGuid()
    testSequenced <| testList "NoteAgg" [
        let withArgs f () =
            go "NoteAgg" |> f
        yield! testFixture withArgs [
            "创建Note", fun finish ->
                let traceId = Guid.NewGuid()
                let command : CreateNote = { Title = "title"; Content = "initial content" }
                app.CreateNote "test" aggId traceId command |> Async.RunSynchronously
                finish 1
            "重复创建Note", fun finish ->
                let traceId = Guid.NewGuid()
                let command : CreateNote = { Title = "title"; Content = "initial content" }
                let f = fun _ -> app.CreateNote "test" aggId traceId command |> Async.RunSynchronously |> ignore
                Expect.throwsC f (fun ex -> printfn "%s" ex.Message)
                finish 2
            "修改Note", fun finish ->
                let traceId = Guid.NewGuid()
                let command : ChangeNote = {Content = "changed content" }
                app.ChangeNote "test" aggId traceId command |> Async.RunSynchronously
                finish 3
        ]
    ]
    |> testLabel "Note App"