namespace UniStream.Domain


module Command =

    let create isValid ctor cv =
        if isValid cv
        then ctor cv
        else failwithf "Invalid value: %A" cv