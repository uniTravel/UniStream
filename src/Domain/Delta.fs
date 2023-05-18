namespace UniStream.Domain

open System
open System.Text.Json


module Delta =

    let inline serialize delta =
        JsonSerializer.SerializeToUtf8Bytes delta |> ReadOnlyMemory

    let inline deserialize (serialized: ReadOnlyMemory<byte>) =
        JsonSerializer.Deserialize serialized.Span
