namespace Benchmark.UniStream

open System
open System.Text
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Diagnosers
open UniStream.Domain


[<AttributeUsage(AttributeTargets.Class)>]
type private JobAttribute () =
    inherit Attribute()
    let cfg =
        ManualConfig.CreateEmpty()
            .AddJob(Job.Default.WithLaunchCount(1).WithWarmupCount(3).WithIterationCount(20).WithId("v0.8.0"))
            .AddDiagnoser(MemoryDiagnoser.Default)
    interface IConfigSource with member _.Config = cfg :> IConfig


[<Job>]
type Basic () =
    let reader, writer, ld, lg = App.config()
    let basic = BasicService (reader, writer, ld, lg)

    [<DefaultValue>] val mutable AggId: string

    [<Params(1, 10)>]
    member val public count = 0 with get, set

    [<IterationSetup>]
    member self.Setup () =
        let traceId = Guid.NewGuid().ToString()
        self.AggId <- Guid.NewGuid().ToString()
        let command : CreateNoteCommand = { Title = "title"; Content = "initial content" }
        basic.CreateNote "benchmark" self.AggId traceId command |> Async.RunSynchronously |> ignore
        let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
        seq { "NoteChanged", Delta.serialize command, metadata }
        |> writer "Benchmark.Direct.Note-" self.AggId UInt64.MaxValue |> Async.RunSynchronously

    [<Benchmark>]
    member self.Write () =
        seq { 0 .. self.count - 1 }
        |> Seq.iter (fun i ->
            basic.ChangeNote "benchmark" self.AggId (Guid.NewGuid().ToString()) { Content = "changed content" } |> Async.RunSynchronously |> ignore)

    [<Benchmark>]
    member self.DirectWrite () =
        seq { 0 .. self.count - 1 }
        |> Seq.iter (fun i ->
            let traceId = Guid.NewGuid().ToString()
            let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
            let command = { Content = "changed content" }
            seq { "NoteCreated", Delta.serialize command, metadata }
            |> writer "Benchmark.Direct.Note-" self.AggId (uint64 i) |> Async.RunSynchronously |> ignore)


[<Job>]
type Batch () =
    let reader, writer, ld, lg = App.config()
    let batch = BatchService (reader, writer, ld, lg)

    [<DefaultValue>] val mutable AggId: string

    [<Params(100, 1000, 5000)>]
    member val public count = 0 with get, set

    [<IterationSetup>]
    member self.Setup () =
        let traceId = Guid.NewGuid().ToString()
        self.AggId <- Guid.NewGuid().ToString()
        let command : CreateNoteCommand = { Title = "title"; Content = "initial content" }
        batch.CreateNote "benchmark" self.AggId traceId command |> Async.RunSynchronously |> ignore
        let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
        seq { "NoteChanged", Delta.serialize command, metadata }
        |> writer "Benchmark.Direct.Note-" self.AggId UInt64.MaxValue |> Async.RunSynchronously

    [<Benchmark>]
    member self.Write () =
        seq { 0 .. self.count - 1 }
        |> Seq.map (fun i -> batch.ChangeNote "benchmark" self.AggId (Guid.NewGuid().ToString()) { Content = "changed content" })
        |> Async.Parallel
        |> Async.RunSynchronously

    [<Benchmark>]
    member self.DirectWrite () =
        let data =
            seq { 0 .. self.count - 1 }
            |> Seq.map (fun i ->
                let traceId = Guid.NewGuid().ToString()
                let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                ("NoteChanged", Delta.serialize { Content = "changed content" }, metadata)
            )
        writer "Benchmark.Direct.Note-" self.AggId 0uL data |> Async.RunSynchronously