namespace UniStream.Domain

open System


[<AbstractClass>]
type Aggregate(id: Guid) as me =

    member val Id = id

    member val Revision = UInt64.MaxValue with get, set

    member _.Next() = me.Revision <- me.Revision + 1UL
