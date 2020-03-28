namespace Benchmark.Base

open BenchmarkDotNet.Running

module Main =

    [<EntryPoint>]
    let main argv =
        let switch = BenchmarkSwitcher [|
            typeof<Serialize>
            typeof<Deserialize>
            typeof<Collections>
        |]
        switch.Run argv |> ignore
        0