namespace Benchmark.Base

open BenchmarkDotNet.Attributes


[<MemoryDiagnoser>]
[<SimpleJob(1, 3, 20)>]
type Pipeline() =

    [<Params(10000, 100000)>]
    member val public count = 0 with get, set

    [<Benchmark>]
    member self.Seq () =
        seq { 1 .. self.count }
        |> Seq.fold (fun l i -> (i, "Test") :: l ) []
        |> List.rev

    [<Benchmark>]
    member self.SeqBack () =
        Seq.foldBack (fun i l -> (i, "Test") :: l ) (seq { 1 .. self.count }) []

    [<Benchmark>]
    member self.SeqScan () =
        seq { 1 .. self.count }
        |> Seq.scan (fun l i -> (i, "Test")) (0, "Test")
        |> Seq.tail
        |> Seq.toList

    [<Benchmark>]
    member self.ListScan () =
        [ 1 .. self.count ]
        |> List.scan (fun l i -> (i, "Test")) (0, "Test")
        |> List.tail

    [<Benchmark>]
    member self.List () =
        [ 1 .. self.count ]
        |> List.fold (fun l i -> (i, "Test") :: l ) []
        |> List.rev