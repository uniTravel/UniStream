module Infrastructure.EventStore.Tests.EventFilter

open System
open System.Text
open System.Threading.Tasks
open Expecto
open EventStore.Client
open UniStream.Infrastructure.EventStore


let createEvents count =
    seq { for i in 1 .. count ->
            let e = Encoding.UTF8.GetBytes ("test" + i.ToString()) |> ReadOnlyMemory
            "Changed", e, Nullable() }

let filter (app: AppService) name =
    let aggType = "Note-"
    let name = "事件过滤_" + name
    let filterOption = StreamFilter.Prefix aggType
    testSequenced <| testList name [
        let withArgs f () =
            let position = app.Position()
            let aggId = Guid.NewGuid().ToString()
            createEvents 1 |> app.Writer aggType aggId UInt64.MaxValue |> Async.RunSynchronously
            createEvents 4 |> app.Writer aggType aggId 0uL |> Async.RunSynchronously
            let completed = TaskCompletionSource<bool>()
            let reached = TaskCompletionSource<bool>()
            let delay = Task.Delay 200
            let count = ref 0
            f position completed reached delay count
        yield! testFixture withArgs [
            "正常订阅", fun position completed reached delay count ->
                let handler aggKey evType version data = async {
                    count := !count + 1
                    if !count = 5 then completed.SetResult true
                    if !count = 6 then reached.SetResult true }
                let option = SubscriptionFilterOptions filterOption
                let filter = app.Filter
                EventFilter.sub filter option position handler |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                EventFilter.unsub app.Filter option |> Async.RunSynchronously
            "处理过程会出错", fun position completed reached delay count ->
                let handler aggKey evType version data = async {
                    count := !count + 1
                    if !count = 2 then failwith "Error"
                    if !count = 6 then completed.SetResult true
                    if !count = 7 then reached.SetResult true }
                let option = SubscriptionFilterOptions filterOption
                let filter = app.Filter
                EventFilter.sub filter option position handler |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                EventFilter.unsub app.Filter option |> Async.RunSynchronously
            "处理过程中退订", fun position completed reached delay count ->
                let hook = TaskCompletionSource<unit>()
                let handler aggKey evType version data = async {
                    count := !count + 1
                    if !count = 2 then hook.SetResult (); Threading.Thread.Sleep 1
                    if !count = 2 then completed.SetResult true
                    if !count = 3 then reached.SetResult true }
                let option = SubscriptionFilterOptions filterOption
                let filter = app.Filter
                EventFilter.sub filter option position handler |> Async.RunSynchronously
                hook.Task |> Async.AwaitTask |> Async.RunSynchronously
                EventFilter.unsub filter option |> Async.Start
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
            "处理过程会出错，然后退订", fun position completed reached delay count ->
                let hook = TaskCompletionSource<unit>()
                let handler aggKey evType version data = async {
                    count := !count + 1
                    if !count = 1 then failwith "Error"
                    if !count = 3 then hook.SetResult (); Threading.Thread.Sleep 1
                    if !count = 3 then completed.SetResult true
                    if !count = 4 then reached.SetResult true }
                let option = SubscriptionFilterOptions filterOption
                let filter = app.Filter
                EventFilter.sub filter option position handler |> Async.RunSynchronously
                hook.Task |> Async.AwaitTask |> Async.RunSynchronously
                EventFilter.unsub filter option |> Async.Start
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                EventFilter.unsub app.Filter option |> Async.RunSynchronously
        ]
    ]

[<Tests>]
let defaultTests = filter EventStoreConfig.app "Grpc客户端"