namespace Benchmark.UniStream

open BenchmarkDotNet.Running


module Main =

    [<EntryPoint>]
    let main argv =
        let switch = BenchmarkSwitcher [|
            typeof<Basic>
            typeof<Batch>
        |]
        switch.Run argv |> ignore
        0