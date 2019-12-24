namespace UniStream.Domain


module Command =

    let create isValid ctor delta =
        if isValid delta
        then ctor delta
        else failwithf "值验证错误：%A" delta

    let inline apply f command =
        f (^c : (member Value: 'd) command)