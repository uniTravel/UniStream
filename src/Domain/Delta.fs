namespace UniStream.Domain

open System
open System.Text.Json

module Delta =

    let inline asBytes delta =
        JsonSerializer.SerializeToUtf8Bytes delta

    let inline fromBytes deltaBytes =
        let span = ReadOnlySpan deltaBytes
        JsonSerializer.Deserialize< ^d> span