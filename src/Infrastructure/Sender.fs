namespace UniStream.Domain

open System
open System.Text.Json


module Sender =

    let inline send<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (sender: ISender<'agg>)
        (aggId: Guid)
        (comId: Guid)
        (com: 'com)
        =
        async {
            match! sender.send aggId comId typeof<'com>.FullName (JsonSerializer.SerializeToUtf8Bytes com) with
            | Ok() -> return ()
            | Error ex -> return raise ex
        }
