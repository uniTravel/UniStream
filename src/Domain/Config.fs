namespace UniStream.Domain

open System


type Get = Guid -> int64 -> (Guid * string * byte[])[] * int64

type EsFunc = Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>

type RepoMode =
    | Cache of int64
    | Snapshot of int64 * int64 * int64

type Repo<'agg> =
    | Take of Guid * AsyncReplyChannel<Result<'agg * int64, string>>
    | Put of Guid * 'agg * int64
    | Refresh
    | Scavenge

type Bat<'agg> =
    | Add of Guid * Guid * ('agg -> byte[] -> (string * byte[] * byte[]) seq * 'agg) * AsyncReplyChannel<string voption>
    | Launch of DiagnoseLog.Logger * Get * EsFunc * MailboxProcessor<Repo<'agg>>
    | Clean of DiagnoseLog.Logger


module Config =

    [<Sealed>]
    type Immutable (esFunc, ldFunc, lgFunc) =
        member _.EsFunc : (string -> Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>) = esFunc
        member _.LdFunc : (string -> string -> byte[] -> byte[] -> unit) = ldFunc
        member _.LgFunc : (string -> byte[] -> unit) = lgFunc

    [<Sealed>]
    type Mutable (get, esFunc, ldFunc, lgFunc, ?cacheMode, ?refresh, ?scavenge, ?threshold, ?batch, ?block) =
        let cacheMode = defaultArg cacheMode true
        let refresh = defaultArg refresh 15L
        let scavenge = defaultArg scavenge 2L
        let threshold = defaultArg threshold 1000L
        let batch = defaultArg batch 55
        let block = defaultArg block 3L
        let repoMode =
            match cacheMode with
            | true -> Cache refresh
            | false -> Snapshot (refresh, scavenge, threshold)
        do
            if refresh <=10L || refresh >= 60L then invalidArg "refresh" "Interval for refresh mutable aggregator's cache must between 10~60 seconds."
            if scavenge <=1L || scavenge >= 24L then invalidArg "scavenge" "Interval for scavenge mutable aggregator's snapshot must between 1~24 hours."
            if block <= 0L || block >= 10L then invalidArg "block" "Block timeout must between 0~10 seconds."

        member _.Get : (string -> Guid -> int64 -> ((Guid * string * byte[])[] * int64)) = get
        member _.EsFunc : (string -> Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>) = esFunc
        member _.LdFunc : (string -> string -> byte[] -> byte[] -> unit) = ldFunc
        member _.LgFunc : (string -> byte[] -> unit) = lgFunc
        member _.RepoMode = repoMode
        member _.Batch = float batch
        member _.BlockTicks = block * 10000000L

    [<Sealed>]
    type Observer (get, ldFunc, lgFunc, subBuilder, observableType: string, ?cacheMode, ?refresh, ?scavenge, ?threshold, ?block) =
        let cacheMode = defaultArg cacheMode true
        let refresh = defaultArg refresh 1800L
        let scavenge = defaultArg scavenge 24L
        let threshold = defaultArg threshold 1000L
        let block = defaultArg block 3L
        let repoMode =
            match cacheMode with
            | true -> Cache refresh
            | false -> Snapshot (refresh, scavenge, threshold)
        do
            if refresh <=1800L || refresh >= 7200L then invalidArg "refresh" "Interval for refresh observer aggregator's cache must between 1800~7200 seconds."
            if scavenge <=24L || scavenge >= 72L then invalidArg "scavenge" "Interval for scavenge observer aggregator's snapshot must between 24~72 hours."
            if block <= 0L || block >= 10L then invalidArg "blockSeconds" "Block timeout must between 0~10 seconds."

        member _.Get : (string -> Guid -> int64 -> ((Guid * string * byte[])[] * int64)) = get
        member _.LdFunc : (string -> string -> byte[] -> byte[] -> unit) = ldFunc
        member _.LgFunc : (string -> byte[] -> unit) = lgFunc
        member _.SubBuilder : ((Guid -> string -> int64 -> byte[] -> byte[] -> Async<unit>) -> unit) = subBuilder
        member _.ObservableType = observableType
        member _.RepoMode = repoMode
        member _.BlockTicks = block * 10000000L