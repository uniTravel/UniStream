namespace UniStream.Domain


module Command =

    let inline create< ^cv, ^c> isValid (ctor: ^cv -> ^c) (cv: ^cv) =
        if isValid cv
        then ctor cv
        else failwithf "Invalid value: %A" cv