namespace UniStream.Domain

open System


[<AbstractClass>]
type Aggregate(id: Guid) as me =
    let apply cmType cm =
        let evType, ev = me.Apply cmType cm
        me.Revision <- me.Revision + 1UL
        evType, ev

    let replay evType ev =
        me.Replay evType ev
        me.Revision <- me.Revision + 1UL

    member val Id = id with get
    member val Revision = UInt64.MaxValue with get, set

    member _.ApplyCommand = apply
    member _.ReplayEvent = replay

    abstract member Apply: cmType: string -> cm: ReadOnlyMemory<byte> -> string * ReadOnlyMemory<byte>

    abstract member Replay: evType: string -> ev: ReadOnlyMemory<byte> -> unit
