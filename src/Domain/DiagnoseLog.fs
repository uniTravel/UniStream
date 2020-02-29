namespace UniStream.Domain

open System.Text.Json


type LogLevel =
    | Trace = 0
    | Debug = 1
    | Info = 2
    | Warn = 3
    | Error = 4
    | Critical = 5


module DiagnoseLog =

    type Logger = { Name: string; LogFunc: string -> byte[] -> unit }

    let logger name logFunc =
        { Name = name; LogFunc = logFunc }

    let log lg level format (stack: string) =
        let doAfter (s: string) =
            {| Level = level; Message = s; StackTrack = stack |}
            |> JsonSerializer.SerializeToUtf8Bytes
            |> lg.LogFunc lg.Name
        Printf.ksprintf doAfter format

    type Logger with
        member this.Trace format = log this LogLevel.Trace format null
        member this.Debug format = log this LogLevel.Debug format null
        member this.Info format = log this LogLevel.Info format null
        member this.Warn format = log this LogLevel.Warn format null
        member this.Error stack format = log this LogLevel.Error format stack
        member this.Critical stack format = log this LogLevel.Critical format stack