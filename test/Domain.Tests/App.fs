[<AutoOpen>]
module Domain.App

open System
open System.Collections.Generic
open UniStream.Domain


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

let create = Aggregator.create<Note, CreateNote, NoteCreated>
let change = Aggregator.apply<Note, ChangeNote, NoteChanged>
let upgrade = Aggregator.apply<Note, UpgradeNote, NoteUpgraded>
