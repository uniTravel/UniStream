namespace UniStream.Domain

open System
open System.Text.Json


module DiagnoseLog =

    [<CLIMutable>]
    type T = { _level: LogLevel; _message: string; _stackTrack: string option }

    type Logger = { _name: string; _logFunc: string -> byte[] -> unit }

    let logger name logFunc =
        { _name = name; _logFunc = logFunc }

    let asBytes (log: T) =
        JsonSerializer.SerializeToUtf8Bytes log

    let fromBytes bytes =
        let span = ReadOnlySpan bytes
        JsonSerializer.Deserialize<T> span

    let log lg level format =
        let doAfter s =
            let d = { _level = level; _message = s; _stackTrack = None } |> asBytes
            lg._logFunc lg._name d
        Printf.ksprintf doAfter format

    let logWithStack lg level stack format =
        let doAfter s =
            let d = { _level = level; _message = s; _stackTrack = Some stack } |> asBytes
            lg._logFunc lg._name d
        Printf.ksprintf doAfter format

    type Logger with
        member this.Trace format = log this LogLevel.Trace format
        member this.Debug format = log this LogLevel.Debug format
        member this.Info format = log this LogLevel.Info format
        member this.Warn format = log this LogLevel.Warn format
        member this.Error stack format = logWithStack this LogLevel.Error stack format
        member this.Critical stack format = logWithStack this LogLevel.Critical stack format