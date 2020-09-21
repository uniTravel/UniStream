module Infrastructure.EventStore.Tests.EventSubscribe

open System
open System.Threading.Tasks
open Expecto
open EventStore.Client
open UniStream.Infrastructure.EventStore


let subscribe (app: AppService) name =
    let aggType = "Note-"
    let name = "事件订阅_" + name
    testSequenced <| testList name [
        let withArgs f () =
            let aggId = Guid.NewGuid().ToString()
            createEvents 1 |> app.Writer aggType aggId UInt64.MaxValue |> Async.RunSynchronously
            createEvents 4 |> app.Writer aggType aggId 0uL |> Async.RunSynchronously
            let completed = TaskCompletionSource<bool>()
            let reached = TaskCompletionSource<bool>()
            let delay = Task.Delay 200
            let count = ref 0
            f aggId completed reached delay count
        yield! testFixture withArgs [
            "正常订阅", fun aggId completed reached delay count ->
                let subscriber = app.EventSubscriber "Note" <| fun aggKey evType version data -> async {
                    count := !count + 1
                    if !count = 4 then completed.SetResult true
                    if !count = 5 then reached.SetResult true }
                EventSubscriber.sub subscriber aggId StreamPosition.Start |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                EventSubscriber.unsub subscriber aggId |> Async.RunSynchronously
            "处理过程会出错", fun aggId completed reached delay count ->
                let subscriber = app.EventSubscriber "Note" <| fun aggKey evType version data -> async {
                    count := !count + 1
                    if !count = 2 then failwith "Error"
                    if !count = 5 then completed.SetResult true
                    if !count = 6 then reached.SetResult true }
                EventSubscriber.sub subscriber aggId StreamPosition.Start |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                EventSubscriber.unsub subscriber aggId |> Async.RunSynchronously
            "处理过程中退订", fun aggId completed reached delay count ->
                let hook = TaskCompletionSource<unit>()
                let subscriber = app.EventSubscriber "Note" <| fun aggKey evType version data -> async {
                    count := !count + 1
                    if !count = 2 then hook.SetResult (); Threading.Thread.Sleep 2
                    if !count = 2 then completed.SetResult true
                    if !count = 3 then reached.SetResult true }
                EventSubscriber.sub subscriber aggId StreamPosition.Start |> Async.RunSynchronously
                hook.Task |> Async.AwaitTask |> Async.RunSynchronously
                EventSubscriber.unsub subscriber aggId |> Async.Start
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
            "处理过程会出错，然后退订", fun aggId completed reached delay count ->
                let hook = TaskCompletionSource<unit>()
                let subscriber = app.EventSubscriber "Note" <| fun aggKey evType version data -> async {
                    count := !count + 1
                    if !count = 1 then failwith "Error"
                    if !count = 3 then hook.SetResult (); Threading.Thread.Sleep 2
                    if !count = 3 then completed.SetResult true
                    if !count = 4 then reached.SetResult true }
                EventSubscriber.sub subscriber aggId StreamPosition.Start |> Async.RunSynchronously
                hook.Task |> Async.AwaitTask |> Async.RunSynchronously
                EventSubscriber.unsub subscriber aggId |> Async.Start
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
        ]
    ]
    |> testLabel "EventStore"

[<Tests>]
let defaultTests = subscribe app "Grpc客户端"