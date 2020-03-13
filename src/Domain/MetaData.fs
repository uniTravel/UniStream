namespace UniStream.Domain

open System
open System.Text


module MetaData =

    let correlationId (id: Guid) =
        let json = "{\"$correlationId\":\"" + id.ToString() + "\"}"
        Encoding.ASCII.GetBytes json