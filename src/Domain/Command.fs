namespace UniStream.Domain

open System.Text.Json


module Command =

    let inline create isValid ctor d =
        if isValid d
        then ctor d
        else failwithf "值验证错误：%A" d

    let inline apply f c =
        f (^c : (member Value: 'd) c)

    let inline asBytes c =
        JsonSerializer.SerializeToUtf8Bytes (^c : (member Value: 'd) c)