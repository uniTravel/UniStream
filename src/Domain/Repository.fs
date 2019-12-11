namespace UniStream.Domain

open System


type Repository<'agg> = {
    Get: string -> Guid -> (byte[] * byte[]) array
    EsFunc: string -> Guid -> string -> byte[] -> byte[] -> unit
    TimeOut: int64
    Map: Map<Guid, 'agg ref>
}


module Repository =

    let empty<'agg> get esFunc timeout : Repository<'agg> =
        { Get = get; EsFunc = esFunc; TimeOut = timeout; Map = Map.empty }

    let take<'agg>
        (repo: Repository<'agg>) (id: Guid) (channel: AsyncReplyChannel<Result<'agg, string>>)
        : Repository<'agg> =
        failwith ""

    let save<'agg>
        (repo: Repository<'agg>) (agg': 'agg) (metaTrace: MetaTrace.T) (delta: byte[])
        : Repository<'agg> =
        failwith ""

    let put<'agg>
        (repo: Repository<'agg>) (agg: 'agg) : Repository<'agg> =
        failwith ""


type Repository<'agg> with

    member this.Take : (Guid -> AsyncReplyChannel<Result<'agg, string>> -> Repository<'agg>) =
        Repository.take this

    member this.Save : ('agg -> MetaTrace.T -> byte[] -> Repository<'agg>) =
        Repository.save this

    member this.Put : ('agg -> Repository<'agg>) =
        Repository.put this