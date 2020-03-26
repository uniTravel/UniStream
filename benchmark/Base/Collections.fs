module Collections

open System.Collections.Generic
open BenchmarkDotNet.Attributes


[<MemoryDiagnoser>]
type Collections () =

    let add a x = a + x

    [<Params(1000000)>]
    member val public count = 0 with get, set

    [<Benchmark>]
    member self.Seq () =
        let mutable a = 0
        let mutable s = Seq.empty
        seq { 1 .. self.count }
        |> Seq.iter (fun i -> s <- seq { i; yield! s })
        seq { 1 .. self.count }
        |> Seq.iter (fun i -> a <- add a i)

    [<Benchmark>]
    member self.Stack () =
        let mutable a = 0
        let stack = new Stack<int>()
        seq { 1 .. self.count }
        |> Seq.iter stack.Push
        seq { 1 .. self.count }
        |> Seq.iter (fun _ -> a <- add a <| stack.Pop())

    [<Benchmark>]
    member self.Queue () =
        let mutable a = 0
        let queue = new Queue<int>()
        seq { 1 .. self.count }
        |> Seq.iter queue.Enqueue
        seq { 1 .. self.count }
        |> Seq.iter (fun _ -> a <- add a <| queue.Dequeue())