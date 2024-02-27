[<AutoOpen>]
module Account.Domain.App

open System
open System.Collections.Generic


let repo = Dictionary<string, (uint64 * string * byte array) list>(10000)

let writer traceId aggType (aggId: Guid) revision evtType evtData =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] <- List.append repo[stream] [ revision + 1UL, evtType, evtData ]
    else
        repo.Add(stream, List.append List.empty [ revision + 1UL, evtType, evtData ])

let reader aggType (aggId: Guid) =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] |> List.map (fun (v, evtType, evtData) -> (evtType, evtData))
    else
        failwith $"The key {stream} is wrong."
