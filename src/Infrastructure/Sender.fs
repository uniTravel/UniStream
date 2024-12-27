namespace UniStream.Domain

open System
open System.Text.Json


type Msg =
    | Send of Guid * Guid * string * byte array * AsyncReplyChannel<Result<unit, exn>>
    | Receive of Guid * Result<unit, exn>
    | Refresh of DateTime


module Sender =

    let timer (interval: float) work =
        let timer = new Timers.Timer(interval)
        timer.AutoReset <- true
        timer.Elapsed.Add work
        async { timer.Start() }

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
