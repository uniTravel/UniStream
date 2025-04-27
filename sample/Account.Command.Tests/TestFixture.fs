[<AutoOpen>]
module Account.TestFixture.Common

open System
open System.Collections.Generic
open System.ComponentModel.DataAnnotations
open System.Threading
open FsCheck
open UniStream.Domain


let cts = new CancellationTokenSource()

let call (com: Async<ComResult>) () =
    async {
        match! com with
        | Fail ex -> raise ex
        | _ -> ()
    }

let validateModel (model: obj) =
    let validationResults = new List<ValidationResult>()
    let context = ValidationContext model

    match Validator.TryValidateObject(model, context, validationResults, true) with
    | true -> seq { }
    | false -> validationResults |> Seq.map (fun r -> r.ErrorMessage)

let validate (model: IValidatableObject) =
    let context = ValidationContext model
    model.Validate context |> Seq.map (fun r -> r.ErrorMessage)

let inline minus<'T> (f: decimal -> 'T) =
    Gen.choose (Int32.MinValue, -1)
    |> Gen.map (fun x -> decimal x / 100m)
    |> Gen.map f

let inline btw<'T> low high (f: decimal -> 'T) =
    Gen.choose (low, high) |> Gen.map (fun x -> decimal x / 100m) |> Gen.map f

let inline gte low (f: decimal -> 'T) =
    Gen.choose (low, Int32.MaxValue)
    |> Gen.map (fun x -> decimal x / 100m)
    |> Gen.map f

let inline scale s low high (f: decimal -> 'T) =
    Gen.choose (low * pown 10 s, high * pown 10 s)
    |> Gen.map (fun x -> decimal x / pown 10m s)
    |> Gen.filter (fun e ->
        let bits = System.Decimal.GetBits e
        bits[3] >>> 16 &&& 0xFF >= s)
    |> Gen.map f

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
