module Base

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    let switch = BenchmarkSwitcher [|
        typeof<Serialize.Serialize>
        typeof<Deserialize.Deserialize>
        typeof<Collections.Collections>
    |]
    switch.Run argv |> ignore
    0