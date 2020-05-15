namespace Benchmark.Base

open System.Collections.Generic
open BenchmarkDotNet.Attributes


[<MemoryDiagnoser>]
[<SimpleJob(1, 3, 20)>]
type Lookup() =

    [<Params(10000, 100000)>]
    member val public count = 0 with get, set

    [<Benchmark>]
    member self.Map () =
        seq { 1 .. self.count }
        |> Seq.fold (fun m i -> Map.add i "Test" m) Map.empty

    [<Benchmark>]
    member self.Dict () =
        let d = Dictionary<int, string>()
        seq { 1 .. self.count }
        |> Seq.iter (fun i -> d.Add(i, "Test"))

    [<Benchmark>]
    member self.DictWithCapacity () =
        let d = Dictionary<int, string>(self.count)
        seq { 1 .. self.count }
        |> Seq.iter (fun i -> d.Add(i, "Test"))