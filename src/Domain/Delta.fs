namespace UniStream.Domain

open System
open System.Text
open System.Text.Json

module Delta =

    let inline asBytes delta =
        let q = Encoding.UTF8
        JsonSerializer.SerializeToUtf8Bytes delta

    let inline fromBytes deltaBytes =
        let span = ReadOnlyMemory deltaBytes
        JsonSerializer.Deserialize< ^d> span.Span