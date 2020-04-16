namespace Benchmark.UniStream

open System
open BenchmarkDotNet.Attributes
open EventStore.ClientAPI
open UniStream.Infrastructure
open UniStream.Domain


[<MemoryDiagnoser>]
type EventStore () =

    let es = Uri "tcp://admin:changeit@localhost:4011"
    let ld = Uri "tcp://admin:changeit@localhost:4012"
    let lg = Uri "tcp://admin:changeit@localhost:4013"
    let app = AppService (es, ld, lg)
    let connect (uri: Uri) =
        let conn = EventStoreConnection.Create uri
        conn.ConnectAsync() |> Async.AwaitTask |> Async.RunSynchronously
        conn
    let c1 = connect es
    let esFunc = DomainEvent.write c1

    [<DefaultValue>] val mutable aggId: Guid

    [<Params(100, 1000)>]
    member val public count = 0 with get, set

    [<IterationSetup>]
    member self.Setup () =
        let traceId = Guid.NewGuid()
        self.aggId <- Guid.NewGuid()
        let command : CreateNote = { Title = "title"; Content = "initial content" }
        app.CreateNote "benchmark" self.aggId traceId command |> Async.RunSynchronously |> ignore

    // [<Benchmark>]
    // member self.Write () =
    //     seq { 1 .. self.count }
    //     |> Seq.iter (fun i ->
    //         app.ChangeNote "benchmark" self.aggId (Guid.NewGuid()) { Content = "changed content" } |> Async.RunSynchronously |> ignore
    //     )

    // [<Benchmark>]
    // member self.DirectWrite () =
    //     seq { 1 .. self.count }
    //     |> Seq.iter (fun i ->
    //         let traceId = Guid.NewGuid().ToString()
    //         let command = { Content = "changed content" }
    //         esFunc "Benchmark.UniStream.Note" (self.aggId.ToString()) (int64 i)
    //         <| seq { ("NoteChanged", Delta.asBytes command, MetaData.correlationId traceId) }
    //         |> Async.Ignore |> Async.RunSynchronously |> ignore
    //     )

    [<Benchmark>]
    member self.BatchWrite () =
        seq { 1 .. self.count }
        |> Seq.map (fun i -> app.BatchChangeNote "benchmark" self.aggId (Guid.NewGuid()) { Content = "changed content" })
        |> Async.Parallel
        |> Async.RunSynchronously

    [<Benchmark>]
    member self.DirectBatchWrite () =
        let data =
            seq { 1 .. self.count }
            |> Seq.map (fun i ->
                let traceId = Guid.NewGuid().ToString()
                ("NoteChanged", Delta.asBytes { Content = "changed content" },  MetaData.correlationId traceId)
            )
        esFunc "Benchmark.UniStream.Note-" (self.aggId.ToString()) 1L data |> Async.RunSynchronously