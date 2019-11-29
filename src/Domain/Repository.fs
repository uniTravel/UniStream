namespace UniStream.Domain

open System


type Repository<'agg when 'agg :> IAggregate> = Repository of Map<Guid, Ref<'agg>>

module Repository =

    let empty<'agg when 'agg :> IAggregate> : Repository<'agg> = Repository <| Map.empty

    let take<'agg when 'agg :> IAggregate>
        (repo: Repository<'agg>) (id: Guid) (get: Guid -> 'agg) (timeout: int64) (channel: AsyncReplyChannel<Result<'agg, string>>)
        : Repository<'agg> =
        let q = 1
        failwith ""

    let put<'agg when 'agg :> IAggregate>
        (repo: Repository<'agg>) (agg: 'agg) (timeout: int64) : Repository<'agg> =
        failwith ""


type Repository<'agg when 'agg :> IAggregate> with

    member this.Take : (Guid -> (Guid -> 'agg) -> int64 -> AsyncReplyChannel<Result<'agg, string>> -> Repository<'agg>) =
        Repository.take this

    member this.Put : ('agg -> int64 -> Repository<'agg>) =
        Repository.put this