module Actor.Tests

open Expecto
open Note.Contract


[<Tests>]
let tests =
    testList "ActorAgg" [
        testCase "Create Actor" <| fun _ ->
            let command = CreateActorCommand()
            command.Name <- "actor"
            let reply = app.CreateActor command |> Async.AwaitTask |> Async.RunSynchronously
            printfn "%s" reply.AggId
    ]
    |> testLabel "Note App"