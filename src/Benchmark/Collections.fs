module Collections

open System.Collections.Generic
open BenchmarkDotNet.Attributes


[<MemoryDiagnoser>]
type Collections () =

    [<Params(1000000)>]
    member val public count = 0 with get, set

    [<Benchmark>]
    member self.List () =
        let mutable list = []
        [ 1 .. self.count ]
        |> List.iter (fun i -> list <- i :: list)
        [ 1 .. self.count ]
        |> List.iter (fun _ -> list <- list.Tail)

    [<Benchmark>]
    member self.Queue () =
        let queue = new Queue<int>()
        [ 1 .. self.count ]
        |> List.iter queue.Enqueue
        [ 1 .. self.count ]
        |> List.iter (fun _ ->
            let q = queue.Dequeue()
            ()
        )