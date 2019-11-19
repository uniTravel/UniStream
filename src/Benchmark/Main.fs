module Benchmark

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    let switch = BenchmarkSwitcher [|
        typeof<Serialize.Serialize>
    |]
    switch.Run argv |> ignore
    0