module Infrastructure.EventStore.Tests.CommandSubscribe

open System
open System.Text.Json
open System.Threading.Tasks
open Expecto
open UniStream.Infrastructure.EventStore


let subscribe (app: AppService) name =
    let name = "命令订阅_" + name
    let launch aggId idx = async {
        let note : CreateNote = { Title = "title" + idx.ToString(); Content = "content" + idx.ToString() }
        let! result = app.CreateNote "Test" aggId note
        Expect.equal result.Content ("content" + idx.ToString()) "返回结果错误" }
    testSequenced <| testList name [
        let withArgs f () =
            let aggId = Guid.NewGuid().ToString()
            let completed = TaskCompletionSource<bool>()
            let reached = TaskCompletionSource<bool>()
            let delay = Task.Delay 200
            let count = ref 0
            f aggId completed reached delay count
        yield! testFixture withArgs [
            "正常订阅", fun aggId completed reached delay count ->
                let subscriber = app.CommandSubscriber
                CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
                <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                    count := !count + 1
                    if !count = 4 then completed.SetResult true
                    if !count = 5 then reached.SetResult true
                    let note = JsonSerializer.Deserialize<CreateNote> data.Span
                    let note = { Title = note.Title; Content = note.Content }
                    callback note }
                |> Async.RunSynchronously
                seq { for i in 1 .. 4 -> launch aggId i } |> Async.Parallel |> Async.RunSynchronously |> ignore
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
            "处理过程会出错", fun aggId completed reached delay count ->
                let subscriber = app.CommandSubscriber
                CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
                <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                    count := !count + 1
                    if !count = 2 then failwith "Error"
                    if !count = 5 then completed.SetResult true
                    if !count = 6 then reached.SetResult true
                    let note = JsonSerializer.Deserialize<CreateNote> data.Span
                    let note = { Title = note.Title; Content = note.Content }
                    callback note }
                |> Async.RunSynchronously
                seq { for i in 1 .. 4 -> launch aggId i } |> Async.Parallel |> Async.RunSynchronously |> ignore
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
            "处理过程中退订", fun aggId completed reached delay count ->
                let hook = TaskCompletionSource<unit>()
                let subscriber = app.CommandSubscriber
                CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
                <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                    count := !count + 1
                    if !count = 2 then hook.SetResult (); Threading.Thread.Sleep 2
                    if !count = 2 then completed.SetResult true
                    if !count = 3 then reached.SetResult true
                    let note = JsonSerializer.Deserialize<CreateNote> data.Span
                    let note = { Title = note.Title; Content = note.Content }
                    callback note }
                |> Async.RunSynchronously
                launch aggId 1 |> Async.RunSynchronously
                launch aggId 2 |> Async.RunSynchronously
                hook.Task |> Async.AwaitTask |> Async.RunSynchronously
                CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                Expect.throws (fun _ -> launch aggId 3 |> Async.RunSynchronously) "应抛出异常"
                Expect.throws (fun _ -> launch aggId 4 |> Async.RunSynchronously) "应抛出异常"
                count := 0
                let completed = TaskCompletionSource<bool>()
                let reached = TaskCompletionSource<bool>()
                let delay = Task.Delay 200
                let subscriber = app.CommandSubscriber
                CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
                <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                    count := !count + 1
                    if !count = 2 then completed.SetResult true
                    if !count = 3 then reached.SetResult true
                    let note = JsonSerializer.Deserialize<CreateNote> data.Span
                    let note = { Title = note.Title; Content = note.Content }
                    callback note }
                |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
            "处理过程会出错，然后退订", fun aggId completed reached delay count ->
                let hook = TaskCompletionSource<unit>()
                let subscriber = app.CommandSubscriber
                CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
                <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                    count := !count + 1
                    if !count = 1 then failwith "Error"
                    if !count = 3 then hook.SetResult (); Threading.Thread.Sleep 2
                    if !count = 3 then completed.SetResult true
                    if !count = 4 then reached.SetResult true
                    let note = JsonSerializer.Deserialize<CreateNote> data.Span
                    let note = { Title = note.Title; Content = note.Content }
                    callback note }
                |> Async.RunSynchronously
                launch aggId 1 |> Async.RunSynchronously
                launch aggId 2 |> Async.RunSynchronously
                hook.Task |> Async.AwaitTask |> Async.RunSynchronously
                CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                Expect.throws (fun _ -> launch aggId 3 |> Async.RunSynchronously) "应抛出异常"
                Expect.throws (fun _ -> launch aggId 4 |> Async.RunSynchronously) "应抛出异常"
                count := 0
                let completed = TaskCompletionSource<bool>()
                let reached = TaskCompletionSource<bool>()
                let delay = Task.Delay 200
                let subscriber = app.CommandSubscriber
                CommandSubscriber.sub<CreateNote, Note> subscriber "CreateNote"
                <| fun cvType traceId user correlationId (data: ReadOnlyMemory<byte>) callback -> async {
                    count := !count + 1
                    if !count = 2 then completed.SetResult true
                    if !count = 3 then reached.SetResult true
                    let note = JsonSerializer.Deserialize<CreateNote> data.Span
                    let note = { Title = note.Title; Content = note.Content }
                    callback note }
                |> Async.RunSynchronously
                let task = Task.WhenAny (completed.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task (completed.Task :> Task) "执行的任务类型错误"
                let task = Task.WhenAny (reached.Task, delay) |> Async.AwaitTask |> Async.RunSynchronously
                Expect.equal task delay "执行了不该执行的任务"
                CommandSubscriber.unsub subscriber "CreateNote" |> Async.RunSynchronously
        ]
    ]
    |> testLabel "EventStore"

[<Tests>]
let defaultTests = subscribe app "Grpc客户端"