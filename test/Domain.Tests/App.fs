[<AutoOpen>]
module Domain.App

open System
open System.Collections.Generic
open UniStream.Domain


let repo = Dictionary<string, (uint64 * string * ReadOnlyMemory<byte>) list>(10000)

let writer aggType (aggId: Guid) (comId: Guid) revision evtType evtData =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] <- List.append repo[stream] [ revision + 1UL, evtType, ReadOnlyMemory evtData ]
    else
        repo.Add(stream, List.append List.empty [ revision + 1UL, evtType, ReadOnlyMemory evtData ])

let reader aggType (aggId: Guid) =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] |> List.map (fun (v, evtType, evtData) -> (evtType, evtData))
    else
        failwith $"The key {stream} is wrong."

let restore aggType ch count = []

let create agent aggId comId com =
    async {
        match! Aggregator.create<Note, CreateNote, NoteCreated> agent aggId comId com with
        | Fail ex -> return raise ex
        | _ -> return ()
    }

let change agent aggId comId com =
    async {
        match! Aggregator.apply<Note, ChangeNote, NoteChanged> agent aggId comId com with
        | Fail ex -> return raise ex
        | _ -> return ()
    }

let upgrade agent aggId comId com =
    async {
        match! Aggregator.apply<Note, UpgradeNote, NoteUpgraded> agent aggId comId com with
        | Fail ex -> return raise ex
        | _ -> return ()
    }
