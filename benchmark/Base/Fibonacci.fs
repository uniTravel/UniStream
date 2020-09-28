namespace Benchmark.Base

open System
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Configs
open BenchmarkDotNet.Jobs
open BenchmarkDotNet.Diagnosers
open Hopac
open Hopac.Infixes


[<AttributeUsage(AttributeTargets.Class)>]
type private JobAttribute () =
    inherit Attribute()
    let cfg =
        ManualConfig.CreateEmpty()
            .AddJob(Job.ShortRun.WithGcServer(true))
            .AddDiagnoser(MemoryDiagnoser.Default)
    interface IConfigSource with member _.Config = cfg :> IConfig


[<Job>]
type Fibonacci () =

    [<Params(20L, 30L)>]
    member val public num = 0L with get, set

    // [<Benchmark>]
    // member self.SerialFun () =
    //     let rec fib n =
    //         if n < 2L then n
    //         else fib (n - 2L) + fib (n - 1L)
    //     fib self.num

    // [<Benchmark>]
    // member self.SerialJob () =
    //     let rec fib n = job {
    //         if n < 2L then return n
    //         else
    //             let! x = fib (n - 2L)
    //             let! y = fib (n - 1L)
    //             return x + y }
    //     run <| fib self.num

    // [<Benchmark>]
    // member self.SerialOpt () =
    //     let rec fib n =
    //         if n < 2L then Job.result n
    //         else
    //             fib <| n - 2L >>= fun x ->
    //             fib <| n - 1L >>- fun y ->
    //             x + y
    //     run <| fib self.num

    [<Benchmark>]
    member self.ParallelJob () =
        let rec fib n = job {
            if n < 2L then return n
            else
                let! (x, y) = fib (n - 2L) <*> fib (n - 1L)
                return x + y }
        run <| fib self.num

    [<Benchmark>]
    member self.ParallelPro () =
        let rec fib n =
            if n < 2L then Job.result n
            else
                fib (n - 2L) |> Promise.start >>= fun xP ->
                fib (n - 1L) >>= fun y ->
                xP >>- fun x ->
                x + y
        run <| fib self.num

    [<Benchmark>]
    member self.ParallelOpt () =
        let rec fib n =
            if n < 2L then Job.result n
            else
                fib (n - 2L) <*> Job.delayWith fib (n - 1L) >>- fun (x, y) ->
                x + y
        run <| fib self.num