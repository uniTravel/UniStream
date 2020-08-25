namespace Benchmark.UniStream

open BenchmarkDotNet.Running


module Main =

    [<EntryPoint>]
    let main argv =
        let switch = BenchmarkSwitcher [|
            typeof<Basic>
            typeof<Batch>
            typeof<Parallel>
            typeof<Immute>
        |]
        switch.Run argv |> ignore
        0