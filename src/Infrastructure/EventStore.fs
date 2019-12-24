namespace UniStream.Infrastructure

open System


module EventStore =

    let getAgg (aggType: string) (aggId: Guid) : (Guid * string * byte[])[] * int64 =
        failwith ""

    let getFromAgg (aggType: string) (aggId: Guid) (version: int64) : (Guid * string * byte[])[] * int64 =
        failwith ""

    let esWrite (aggType: string) (aggId: Guid) (version: int64) (traceId: Guid) (deltaType: string) (delta: byte[]) : unit =
        failwith ""

    let ldWrite (aggType: string) (traceId: Guid) (deltaType: string) (dLog: byte[]) : unit =
        failwith ""

    let lgWrite (stream: string) (gLog: byte[]) : unit =
        failwith ""