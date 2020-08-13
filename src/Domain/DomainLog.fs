namespace UniStream.Domain

open System
open System.Text.Json


module DomainLog =

    type T = { AggType: string; LogFunc: string -> string -> ReadOnlyMemory<byte> -> Async<unit> }

    let logger aggType logFunc =
        { AggType = aggType; LogFunc = logFunc }

    let agent =
        MailboxProcessor<T * string * string * string * string * string * string>.Start <| fun inbox ->
            let rec loop () = async {
                let! lg, user, cvType, status, aggKey, traceId, s = inbox.Receive()
                {| AggType = lg.AggType; AggId = aggKey; TraceId = traceId; Status = status; Message = s |}
                |> JsonSerializer.SerializeToUtf8Bytes |> ReadOnlyMemory |> lg.LogFunc user cvType |> Async.RunSynchronously
                return! loop ()
            }
            loop ()

    let log lg user cvType status aggKey traceId format =
        let doAfter (s: string) = agent.Post (lg, user, cvType, status, aggKey, traceId, s)
        Printf.ksprintf doAfter format

    type T with
        member this.Process user cvType aggKey traceId format = log this user cvType "process" aggKey traceId format
        member this.Success user cvType aggKey traceId format = log this user cvType "success" aggKey traceId format
        member this.Fail user cvType aggKey traceId format = log this user cvType "fail" aggKey traceId format