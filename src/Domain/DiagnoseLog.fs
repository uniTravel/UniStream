namespace UniStream.Domain

open System
open System.Text.Json


type LogLevel =
    | Trace = 0
    | Debug = 1
    | Info = 2
    | Warn = 3
    | Error = 4
    | Critical = 5


module DiagnoseLog =

    type T = { Name: string; LogFunc: string -> ReadOnlyMemory<byte> -> Async<unit> }

    let logger name logFunc =
        { Name = name; LogFunc = logFunc }

    let agent =
        MailboxProcessor<T * LogLevel * string * string>.Start <| fun inbox ->
            let rec loop () = async {
                let! lg, level, s, stack = inbox.Receive()
                {| Level = level; Message = s; StackTrace = stack |}
                |> JsonSerializer.SerializeToUtf8Bytes |> ReadOnlyMemory |> lg.LogFunc lg.Name |> Async.RunSynchronously
                return! loop ()
            }
            loop ()

    let log lg level format stack =
        let doAfter s = agent.Post (lg, level, s, stack)
        Printf.ksprintf doAfter format

    type T with
        member this.Trace format = log this LogLevel.Trace format null
        member this.Debug format = log this LogLevel.Debug format null
        member this.Info format = log this LogLevel.Info format null
        member this.Warn format = log this LogLevel.Warn format null
        member this.Error stack format = log this LogLevel.Error format stack
        member this.Critical stack format = log this LogLevel.Critical format stack