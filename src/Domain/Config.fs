namespace UniStream.Domain


type Reader = string -> int64 -> (string * byte[])[] * int64

type Writer = string -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>

type SubHandler = string -> string -> int64 -> byte[] -> byte[] -> Async<unit>

type SubDropHandler = string -> exn -> Async<unit>

type SubBuilder = string -> SubHandler -> SubDropHandler -> (unit -> unit)

type RepoMode =
    | Cache of int * int * int64
    | Snapshot of int * int * int64 * int64 * int64


module Config =

    [<Sealed>]
    type Immutable (esFunc, ldFunc, lgFunc) =
        member _.EsFunc : (string -> string -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>) = esFunc
        member _.LdFunc : (string -> string -> byte[] -> byte[] -> unit) = ldFunc
        member _.LgFunc : (string -> byte[] -> unit) = lgFunc

    [<Sealed>]
    type Mutable (get, esFunc, ldFunc, lgFunc, ?cacheMode, ?capacity, ?keep, ?refresh, ?scavenge, ?threshold, ?batch) =
        let cacheMode = defaultArg cacheMode true
        let capacity = defaultArg capacity 10000
        let keep = defaultArg keep 3000
        let refresh = defaultArg refresh 15
        let scavenge = defaultArg scavenge 24
        let threshold = defaultArg threshold 1000
        let batch = defaultArg batch 19
        let repoMode =
            match cacheMode with
            | true -> Cache (capacity, keep, int64 refresh)
            | false -> Snapshot (capacity, keep, int64 refresh, int64 scavenge, int64 threshold)
        do
            if capacity < 5000 || capacity > 100000 then invalidArg "capacity" "Capacity of mutable aggregator's repository must between 5000~100000."
            if refresh < 10 || refresh > 180 then invalidArg "refresh" "Interval for refresh mutable aggregator's cache must between 10~180 seconds."
            if scavenge < 24 || scavenge > 72 then invalidArg "scavenge" "Interval for scavenge mutable aggregator's snapshot must between 24~72 hours."

        member _.Get : (string -> string -> int64 -> ((string * byte[])[] * int64)) = get
        member _.EsFunc : (string -> string -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>) = esFunc
        member _.LdFunc : (string -> string -> byte[] -> byte[] -> unit) = ldFunc
        member _.LgFunc : (string -> byte[] -> unit) = lgFunc
        member _.RepoMode = repoMode
        member _.Batch = float batch

    [<Sealed>]
    type Observer (get, lgFunc, subBuilder, ?prefix, ?cacheMode, ?capacity, ?keep, ?refresh, ?scavenge, ?threshold) =
        let prefix = defaultArg prefix "$bc-"
        let cacheMode = defaultArg cacheMode true
        let capacity = defaultArg capacity 10000
        let keep = defaultArg keep 5000
        let refresh = defaultArg refresh 30
        let scavenge = defaultArg scavenge 24
        let threshold = defaultArg threshold 1000
        let repoMode =
            match cacheMode with
            | true -> Cache (capacity, keep, int64 refresh)
            | false -> Snapshot (capacity, keep, int64 refresh, int64 scavenge, int64 threshold)
        do
            if capacity < 5000 || capacity > 100000 then invalidArg "capacity" "Capacity of observer aggregator's repository must between 5000~100000."
            if refresh < 30 || refresh > 3600 then invalidArg "refresh" "Interval for refresh observer aggregator's cache must between 30~3600 minutes."
            if scavenge < 24 || scavenge > 72 then invalidArg "scavenge" "Interval for scavenge observer aggregator's snapshot must between 24~72 hours."

        member _.Reader : Reader = get prefix
        member _.LgFunc : (string -> byte[] -> unit) = lgFunc
        member _.SubBuilder : SubBuilder = subBuilder prefix
        member _.RepoMode = repoMode