namespace Benchmark.UniStream

open BenchmarkDotNet.Running


module Main =

    [<EntryPoint>]
    let main argv =
        let switch = BenchmarkSwitcher [|
            typeof<EventStore>
        |]
        switch.Run argv |> ignore
        0