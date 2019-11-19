module Benchmark

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    let switch = BenchmarkSwitcher [|
        typeof<Serialize.Serialize>
        typeof<Deserialize.Deserialize>
    |]
    switch.Run argv |> ignore
    0