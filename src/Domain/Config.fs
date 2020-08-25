namespace UniStream.Domain

open System


module Config =

    [<Sealed>]
    type Immutable
        (esFunc: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>,
         ldFunc: string -> string -> ReadOnlyMemory<byte> -> Async<unit>,
         lgFunc: string -> ReadOnlyMemory<byte> -> Async<unit>) =

        member _.EsFunc aggType aggKey version eData = esFunc aggType aggKey version eData
        member _.LdFunc user category data = ldFunc user category data
        member _.LgFunc aggType data = lgFunc aggType data


    [<Sealed>]
    type Mutable
        (get: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         esFunc: string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>,
         ldFunc: string -> string -> ReadOnlyMemory<byte> -> Async<unit>,
         lgFunc: string -> ReadOnlyMemory<byte> -> Async<unit>,
         ?capacity, ?keep, ?refresh, ?batch, ?scavenge, ?threshold) =

        let capacity = defaultArg capacity 10000
        let keep = defaultArg keep 3000
        let refresh = defaultArg refresh 15u
        let batch = defaultArg batch 0u
        let scavenge = defaultArg scavenge 0u
        let threshold = defaultArg threshold 1000uL
        do
            if capacity < 5000 || capacity > 100000 then
                invalidArg "capacity" "Capacity of mutable aggregator's repository must between 5000~100000."
            if refresh < 10u || refresh > 180u then
                invalidArg "refresh" "Interval for refresh mutable aggregator's cache must between 10~180 seconds."
            if (scavenge < 24u && scavenge > 0u) || scavenge > 72u then
                invalidArg "scavenge" "Interval for scavenge mutable aggregator's snapshot must be 0 or between 24~72 hours."

        member _.Get aggType aggKey version = get aggType aggKey version
        member _.EsFunc aggType aggKey version eData = esFunc aggType aggKey version eData
        member _.LdFunc user category data = ldFunc user category data
        member _.LgFunc aggType data = lgFunc aggType data
        member _.Capacity = capacity
        member _.Keep = keep
        member _.Refresh = refresh
        member _.Batch = batch
        member _.Scavenge = scavenge
        member _.Threshold = threshold


    [<Sealed>]
    type Observer
        (get: string -> string -> uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>,
         lgFunc: string -> ReadOnlyMemory<byte> -> Async<unit>,
         ?capacity, ?keep, ?refresh, ?scavenge, ?threshold) =
        let capacity = defaultArg capacity 10000
        let keep = defaultArg keep 5000
        let refresh = defaultArg refresh 30u
        let scavenge = defaultArg scavenge 0u
        let threshold = defaultArg threshold 1000uL
        do
            if capacity < 5000 || capacity > 100000 then
                invalidArg "capacity" "Capacity of observer aggregator's repository must between 5000~100000."
            if refresh < 30u || refresh > 3600u then
                invalidArg "refresh" "Interval for refresh observer aggregator's cache must between 30~3600 minutes."
            if (scavenge < 24u && scavenge > 0u) || scavenge > 72u then
                invalidArg "scavenge" "Interval for scavenge observer aggregator's snapshot must be 0 or between 24~72 hours."

        member _.Get aggType aggKey version = get aggType aggKey version
        member _.LgFunc aggType data = lgFunc aggType data
        member _.Capacity = capacity
        member _.Keep = keep
        member _.Refresh = refresh
        member _.Scavenge = scavenge
        member _.Threshold = threshold