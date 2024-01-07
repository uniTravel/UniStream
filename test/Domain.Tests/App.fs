[<AutoOpen>]
module Domain.App

open System
open System.Collections.Generic
open UniStream.Domain


let repo = Dictionary<string, seq<uint64 * string * ReadOnlyMemory<byte>>>(10000)

let writer aggType (aggId: Guid) revision comType comData =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] <- Seq.append repo[stream] [ revision + 1UL, comType, comData ]
    else
        repo.Add(stream, Seq.append Seq.empty [ revision + 1UL, comType, comData ])

let reader aggType (aggId: Guid) =
    let stream = aggType + "-" + aggId.ToString()

    if repo.ContainsKey stream then
        repo[stream] |> Seq.map (fun (v, comType, comData) -> (comType, comData))
    else
        failwith $"The key {stream} is wrong."

let create = Aggregator.create<Note, CreateNote>
let change = Aggregator.apply<Note, ChangeNote>
let upgrade = Aggregator.apply<Note, UpgradeNote>
