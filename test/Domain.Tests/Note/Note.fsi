namespace Domain

open System
open UniStream.Domain


[<Sealed>]
type Note =
    inherit Aggregate

    new: id: Guid -> Note

    member Title: string with get

    member internal Title: string with set

    member Content: string with get

    member internal Content: string with set

    member Grade: int with get

    member internal Grade: int with set
