namespace UniStream.Domain

open System
open System.Text.Json


module DomainEvent =

    let create<'v, 'agg, 'e when 'v :> IValue and 'agg :> IAggregate and 'e :> IDomainEvent<'v, 'agg>> (ctor: 'v -> 'e) e =
        ctor e

    let apply<'v, 'a when 'v :> IValue> (f: 'v -> 'a) (e: IWrapped<'v>) =
        e.Value |> f

    let value e =
        apply id e

    let equals left right =
        (value left) = (value right)

    let asBytes<'v when 'v :> IValue> (e: 'v) =
        JsonSerializer.SerializeToUtf8Bytes e

    let fromBytes<'v when 'v :> IValue> (bytes: byte[]) : 'v =
        let span = ReadOnlySpan bytes
        JsonSerializer.Deserialize<'v> span