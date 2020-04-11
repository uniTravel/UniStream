namespace UniStream.Domain

open System.Text


module MetaData =

    let correlationId id =
        let json = "{\"$correlationId\":\"" + id + "\"}"
        Encoding.ASCII.GetBytes json