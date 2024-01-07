namespace Domain

open UniStream.Domain


[<Sealed>]
type Note(id) =
    inherit Aggregate(id)

    member val Title = "" with get, set
    member val Content = "" with get, set
    member val Grade = 0 with get, set
