module Domain.Tests

open System
open System.Text.Json
open Expecto

[<CLIMutable>]
type MetaEvent = { AggregateId: Guid; TraceId: Guid; Version: int }

[<Tests>]
let tests =
    testList "Domain" [
        testCase "1" <| fun _ ->
            let e = { AggregateId = Guid.NewGuid (); TraceId = Guid.NewGuid (); Version = 7 }
            let array = JsonSerializer.SerializeToUtf8Bytes e
            let span = ReadOnlySpan array
            let q = JsonSerializer.Deserialize<MetaEvent> span
            ()
    ]