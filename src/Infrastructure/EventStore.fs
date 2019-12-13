namespace UniStream.Infrastructure

open System


module EventStore =

    let getAgg (stream: string) (aId: Guid) : (byte[] * byte[])[] =
        failwith ""

    let esWrite (stream: string) (tId: Guid) (delta: string) (metaEvent: byte[]) (event: byte[]) : unit =
        failwith ""

    let ldWrite (stream: string) (tId: Guid) (delta: string) (metaTrace: byte[]) (dLog: byte[]) : unit =
        failwith ""

    let lgWrite (stream: string) (gLog: byte[]) : unit =
        failwith ""