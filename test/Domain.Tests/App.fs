[<AutoOpen>]
module Domain.App

open System
open System.Collections.Generic
open UniStream.Domain


let restored =
    [ Guid "a4f374f2-718d-41ef-8ee3-5ffa89704c8c"
      Guid "8c2bd911-ba24-459b-a7dd-da07c84b0f5f"
      Guid "68122bb7-4e68-41bb-89cc-217e6e254c8c" ]

let writer
    (repo: Dictionary<string, (uint64 * string * ReadOnlyMemory<byte>) list>)
    aggType
    (aggId: Guid)
    (comId: Guid)
    revision
    evtType
    evtData
    =
    let stream = aggType + "-" + aggId.ToString()

    try
        if repo.ContainsKey stream then
            if revision = UInt64.MaxValue then
                failwith $"The revision {revision} is wrong"
            else
                repo[stream] <- List.append repo[stream] [ revision + 1UL, evtType, ReadOnlyMemory evtData ]
        else
            repo.Add(stream, List.append List.empty [ revision + 1UL, evtType, ReadOnlyMemory evtData ])
    with ex ->
        raise <| WriteException($"Write stream of {aggId} error", ex)

let reader (repo: Dictionary<string, (uint64 * string * ReadOnlyMemory<byte>) list>) aggType (aggId: Guid) =
    let stream = aggType + "-" + aggId.ToString()

    try
        if repo.ContainsKey stream then
            repo[stream] |> List.map (fun (v, evtType, evtData) -> (evtType, evtData))
        else
            failwith $"The key {stream} is wrong."
    with ex ->
        raise <| ReadException($"Read strem of {aggId} error", ex)

let restore (aggType: string) (ch: HashSet<Guid>) (count: int) =
    restored |> List.iter (fun r -> ch.Add r |> ignore)
    restored

let call (com: Async<ComResult>) () =
    async {
        match! com with
        | Fail ex -> raise ex
        | _ -> ()
    }

let create agent aggId comId com =
    Aggregator.create<Note, CreateNote, NoteCreated> agent aggId comId com

let change agent aggId comId com =
    Aggregator.apply<Note, ChangeNote, NoteChanged> agent aggId comId com

let upgrade agent aggId comId com =
    Aggregator.apply<Note, UpgradeNote, NoteUpgraded> agent aggId comId com

let get agent aggId = Aggregator.get<Note> agent aggId
