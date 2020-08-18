namespace UniStream.Domain

open System
open System.Timers
open System.Text


module Factory =

    let inline reply (channel: AsyncReplyChannel<Result< ^agg, string>>) result =
        async { channel.Reply result }

    let inline build (lg: DiagnoseLog.T) writer (agg: ^agg) (version: uint64) (cvType, data, metadata: Nullable<ReadOnlyMemory<byte>>, channel) =
        lg.Trace "Apply command to mutable aggregate and write to stream."
        let apply = (^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg)) agg)
        let (events: (string * ReadOnlyMemory<byte>) seq), (agg': ^agg) =
            try apply cvType data
            with ex ->
                lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
                raise <| ApplyCommandException ex.Message
        let eData = events |> Seq.map (fun (evType, data) -> evType, data, metadata)
        writer version eData |> Async.RunSynchronously
        Ok agg' |> reply channel |> Async.Start
        agg', Seq.length events

    let inline batch (lg: DiagnoseLog.T) writer  (agg: ^agg) (version: uint64) cmds =
        lg.Trace "Batch apply commands to mutable aggregate and write to stream."
        let apply = (^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg)) agg)
        let result, agg' =
            cmds
            |> List.rev
            |> List.mapFold (fun agg (cvType, data, metadata: Nullable<ReadOnlyMemory<byte>>, channel) ->
                let (events: (string * ReadOnlyMemory<byte>) seq), (agg: ^agg), result =
                    try
                        let events, agg = apply cvType data
                        events, agg, Ok agg
                    with ex ->
                        lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
                        Seq.empty, agg, Error ex.Message
                let eData = events |> Seq.map (fun (evType, data) -> evType, data, metadata)
                let reply = reply channel result
                (eData, reply), agg
            ) agg
        let eData, reply = List.unzip result
        let eData = Seq.collect id eData
        writer version eData |> Async.RunSynchronously
        Async.Start <| async { reply |> Async.Parallel |> Async.RunSynchronously |> ignore }
        agg', Seq.length eData

    let inline raw (lg: DiagnoseLog.T) reader snapshot =
        let agg, version, (events: (uint64 * string * ReadOnlyMemory<byte>) seq) =
            match snapshot with
            | ValueSome (agg, version) -> agg, version, reader (version + 1uL)
            | ValueNone -> (^agg : (static member Initial : ^agg) ()), UInt64.MaxValue, reader 0uL
        let len = Seq.length events
        lg.Trace "Get %d events from store for observe aggregate." len
        match version, len with
        | UInt64.MaxValue, 0 -> failwith "Raw data not found."
        | _, 0 -> agg, version
        | _, len ->
            Seq.fold (fun agg elem ->
                let (_, evType, data) = elem
                (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
            ) agg events, version + uint64 len

    let inline init (lg: DiagnoseLog.T) reader writer snapshot cmd =
        let agg, version, (events: (uint64 * string * ReadOnlyMemory<byte>) seq) =
            match snapshot with
            | ValueSome (agg, version) -> agg, version, reader (version + 1uL)
            | ValueNone -> (^agg : (static member Initial : ^agg) ()), UInt64.MaxValue, reader 0uL
        let len = Seq.length events
        lg.Trace "Get %d events from stream for init aggregate." len
        match version, len with
        | UInt64.MaxValue, 0 ->
            let agg', len = build lg writer agg version cmd
            agg', uint64 len - 1uL
        | _, 0 ->
            let agg', len = build lg writer agg version cmd
            agg', version + uint64 len
        | _, len ->
            let agg, version =
                Seq.fold (fun agg elem ->
                    let (_, evType, data) = elem
                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                ) agg events, version + uint64 len
            let agg', len = build lg writer agg version cmd
            agg', version + uint64 len


module Basic =

    type Msg<'agg> =
        | Post of string * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Get of AsyncReplyChannel<Result<'agg, string>>

    let inline run lg reader writer agg version cmd =
        try Factory.build lg writer agg version cmd
        with
        | ApplyCommandException ex -> failwith ex
        | ex ->
            lg.Error ex.StackTrace "Save events failed: %s" ex.Message
            let events : (uint64 * string * ReadOnlyMemory<byte>) seq = reader <| version + 1uL
            let agg, version =
                Seq.fold (fun agg elem ->
                    let (_, evType, data) = elem
                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                ) agg events, version + uint64 (Seq.length events)
            Factory.build lg writer agg version cmd

    let inline agent (lg: DiagnoseLog.T) reader writer agg version shot =
        MailboxProcessor<Msg< ^agg>>.Start <| fun inbox ->
            let rec loop agg version closed = async {
                match! inbox.Receive() with
                | Post (cvType, traceId, data, channel) ->
                    if closed then
                        lg.Warn "Aggregate closed."
                        Error "Aggregate closed." |> Factory.reply channel |> Async.Start
                    else
                        try
                            let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                            let agg', len = run lg reader writer agg version (cvType, data, metadata, channel)
                            let version' = version + uint64 len
                            match shot with
                            | None -> ()
                            | Some shot -> shot agg' version' |> Async.Start
                            return! loop agg' version' (^agg : (member Closed : bool) agg')
                        with ex ->
                            lg.Error ex.StackTrace "Apply command to stream failed: %s" ex.Message
                            Error ex.Message |> Factory.reply channel |> Async.Start
                | Get channel -> Ok agg |> Factory.reply channel |> Async.Start
                return! loop agg version closed }
            loop agg version (^agg : (member Closed : bool) agg)

    let inline post (agent: MailboxProcessor<Msg< ^agg>>) cvType traceId data channel =
        agent.Post <| Post (cvType, traceId, data, channel)

    let inline get (agent: MailboxProcessor<Msg< ^agg>>) channel =
        agent.Post <| Get channel


module Batched =

    type Msg<'agg> =
        | Add of string * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Launch of Timer
        | Get of AsyncReplyChannel<Result<'agg, string>>

    let inline run lg reader writer agg version cmd =
        try Factory.batch lg writer agg version cmd
        with ex ->
            lg.Error ex.StackTrace "Save events failed: %s" ex.Message
            let events : (uint64 * string * ReadOnlyMemory<byte>) seq = reader <| version + 1uL
            let agg, version =
                Seq.fold (fun agg elem ->
                    let (_, evType, data) = elem
                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                ) agg events, version + uint64 (Seq.length events)
            Factory.batch lg writer agg version cmd

    let inline agent (lg: DiagnoseLog.T) reader writer interval agg version shot =
        let agent = new MailboxProcessor<Msg< ^agg>> (fun inbox ->
            let rec loop agg version closed batch = async {
                match! inbox.Receive() with
                | Add (cvType, traceId, data, channel) ->
                    if closed then
                        lg.Warn "Aggregate closed."
                        Error "Aggregate closed." |> Factory.reply channel |> Async.Start
                    else
                        let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                        return! loop agg version closed <| (cvType, data, metadata, channel) :: batch
                | Launch timer ->
                    if not batch.IsEmpty then
                        try
                            let agg', len = run lg reader writer agg version batch
                            let version' = version + uint64 len
                            match shot with
                            | None -> ()
                            | Some shot -> shot agg' version' |> Async.Start
                            let closed' = (^agg : (member Closed : bool) agg')
                            if closed' then timer.Stop()
                            lg.Trace "Launch batch successed, applied %d commands." batch.Length
                            return! loop agg' version' closed' []
                        with ex ->
                            lg.Error ex.StackTrace "Launch batch failed: %s" ex.Message
                            batch
                            |> List.map (fun (_, _, _, channel) -> Factory.reply channel <| Error ex.Message)
                            |> Async.Parallel |> Async.RunSynchronously |> ignore
                            return! loop agg version closed []
                | Get channel -> Ok agg |> Factory.reply channel |> Async.Start
                return! loop agg version closed batch }
            loop agg version (^agg : (member Closed : bool) agg) [])
        agent.Start()
        Async.Start <| async {
            let timer = new Timer (interval)
            timer.AutoReset <- true
            timer.Elapsed.Add (fun _ -> agent.Post <| Launch timer )
            timer.Start() }
        agent

    let inline post (agent: MailboxProcessor<Msg< ^agg>>) cvType traceId data channel =
        agent.Post <| Add (cvType, traceId, data, channel)

    let inline get (agent: MailboxProcessor<Msg< ^agg>>) channel =
        agent.Post <| Get channel


module Observed =

    type Msg<'agg> =
        | Append of uint64 * string * ReadOnlyMemory<byte>
        | Get of AsyncReplyChannel<Result<'agg, string>>

    let inline agent (lg: DiagnoseLog.T) reader agg version shot =
        MailboxProcessor<Msg< ^agg>>.Start <| fun inbox ->
            let rec loop agg version = async {
                match! inbox.Receive() with
                | Append (number, evType, data) ->
                    lg.Trace "Append event to observed aggregate."
                    try
                        match number - version with
                        | 1uL ->
                            let agg' = (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                            let version' = version + 1uL
                            match shot with
                            | None -> ()
                            | Some shot -> shot agg' version' |> Async.Start
                            return! loop agg' version'
                        | d when d > 1uL ->
                            let events : (uint64 * string * ReadOnlyMemory<byte>) seq = reader <| version + 1uL
                            let agg', version' =
                                Seq.fold (fun agg elem ->
                                    let (_, evType, data) = elem
                                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                                ) agg events, version + uint64 (Seq.length events)
                            match shot with
                            | None -> ()
                            | Some shot -> shot agg' version' |> Async.Start
                            return! loop agg' version'
                        | _ -> ()
                    with ex -> lg.Error ex.StackTrace "Update aggregate failed: %s" ex.Message
                | Get channel -> Ok agg |> Factory.reply channel |> Async.Start
                return! loop agg version }
            loop agg version

    let inline append (agent: MailboxProcessor<Msg< ^agg>>) number evType data =
        agent.Post <| Append (number, evType, data)

    let inline get (agent: MailboxProcessor<Msg< ^agg>>) channel =
        agent.Post <| Get channel