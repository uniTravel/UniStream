namespace UniStream.Domain

open System
open System.Text.Json


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
            match!
                sender.Agent.PostAndAsyncReply
                <| fun channel ->
                    Send(aggId, comId, typeof<'com>.FullName, JsonSerializer.SerializeToUtf8Bytes com, channel)
            with
            | Ok() -> return ()
            | Error ex -> return raise ex
        }
