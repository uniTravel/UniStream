[<AutoOpen>]
module Domain.App

open System
open System.Collections.Generic
open UniStream.Domain


let repo = Dictionary<string, seq<uint64 * string * ReadOnlyMemory<byte>>>(10000)

let writer traceId aggType (aggId: Guid) revision chgType chgData =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] <- Seq.append repo[stream] [ revision + 1UL, chgType, chgData ]
    else
        repo.Add(stream, Seq.append Seq.empty [ revision + 1UL, chgType, chgData ])

let reader aggType (aggId: Guid) =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] |> Seq.map (fun (v, chgType, chgData) -> (chgType, chgData))
    else
        failwith $"The key {stream} is wrong."

let create = Aggregator.create<Note, CreateNote>
let change = Aggregator.apply<Note, ChangeNote>
let upgrade = Aggregator.apply<Note, UpgradeNote>
