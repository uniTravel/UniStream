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

    type T = { Level: LogLevel; Message: string; StackTrack: string option }

    type Logger = { Name: string; LogFunc: string -> byte[] -> unit }

    let logger name logFunc =
        { Name = name; LogFunc = logFunc }

    let asBytes (log: T) =
        JsonSerializer.SerializeToUtf8Bytes log

    let log lg level format =
        let doAfter s =
            let d = { Level = level; Message = s; StackTrack = None } |> asBytes
            lg.LogFunc lg.Name d
        Printf.ksprintf doAfter format

    let logWithStack lg level stack format =
        let doAfter s =
            let d = { Level = level; Message = s; StackTrack = Some stack } |> asBytes
            lg.LogFunc lg.Name d
        Printf.ksprintf doAfter format

    type Logger with
        member this.Trace format = log this LogLevel.Trace format
        member this.Debug format = log this LogLevel.Debug format
        member this.Info format = log this LogLevel.Info format
        member this.Warn format = log this LogLevel.Warn format
        member this.Error stack format = logWithStack this LogLevel.Error stack format
        member this.Critical stack format = logWithStack this LogLevel.Critical stack format