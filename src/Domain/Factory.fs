namespace UniStream.Domain

open System
open System.Timers
open System.Text


module Factory =

    let inline reply (channel: AsyncReplyChannel<Result< ^agg, string>>) result =
        async { channel.Reply result }

    let inline build (lg: DiagnoseLog.T) writer (agg: ^agg) (version: uint64) (apply, metadata: Nullable<ReadOnlyMemory<byte>>, channel) = async {
        let (events: (string * ReadOnlyMemory<byte>) seq), (agg': ^agg) =
            try apply agg
            with ex ->
                lg.Error ex.StackTrace "Apply command failed: %s" ex.Message
                raise <| ApplyCommandException ex.Message
        let eData = events |> Seq.map (fun (evType, data) -> evType, data, metadata)
        do! writer version eData
        Ok agg' |> reply channel |> Async.Start
        return agg', Seq.length events }

    let inline batch (lg: DiagnoseLog.T) writer (agg: ^agg) (version: uint64) cmds = async {
        let result, agg' =
            cmds
            |> List.rev
            |> List.mapFold (fun agg (apply, metadata: Nullable<ReadOnlyMemory<byte>>, channel) ->
                let (events: (string * ReadOnlyMemory<byte>) seq), (agg: ^agg), result =
                    try
                        let events, agg = apply agg
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
        return agg', Seq.length eData }

    let inline raw (lg: DiagnoseLog.T) reader snapshot = async {
        let agg, version, (events: (uint64 * string * ReadOnlyMemory<byte>) seq) =
            match snapshot with
            | ValueSome (agg, version) -> agg, version, Async.RunSynchronously <| reader (version + 1uL)
            | ValueNone -> (^agg : (static member Initial : ^agg) ()), UInt64.MaxValue, Async.RunSynchronously <| reader 0uL
        let len = Seq.length events
        lg.Trace "Get %d events from store for observe aggregate." len
        let agg, version =
            match version, len with
            | UInt64.MaxValue, 0 -> failwith "Raw data not found."
            | _, 0 -> agg, version
            | _, len ->
                Seq.fold (fun agg elem ->
                    let (_, evType, data) = elem
                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                ) agg events, version + uint64 len
        return agg, version }

    let inline init (lg: DiagnoseLog.T) (reader: uint64 -> Async<(uint64 * string * ReadOnlyMemory<byte>) seq>) writer (snapshot: (^agg * uint64) voption) cmd = async {
        let agg, version, start =
            match snapshot with
            | ValueSome (agg, version) -> agg, version, (version + 1uL)
            | ValueNone -> (^agg : (static member Initial : ^agg) ()), UInt64.MaxValue, 0uL
        let! (events: (uint64 * string * ReadOnlyMemory<byte>) seq) = reader start
        let len = Seq.length events
        lg.Trace "Get %d events from store." len
        match version, len with
        | UInt64.MaxValue, 0 ->
            let! agg', len = build lg writer agg version cmd
            return agg', uint64 len - 1uL
        | _, 0 ->
            let! agg', len = build lg writer agg version cmd
            return agg', version + uint64 len
        | _, len ->
            let agg, version =
                Seq.fold (fun agg elem ->
                    let (_, evType, data) = elem
                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                ) agg events, version + uint64 len
            let! agg', len = build lg writer agg version cmd
            return agg', version + uint64 len }


module Basic =

    type Msg<'agg> =
        | Init of string * ('agg -> (string * ReadOnlyMemory<byte>) seq * 'agg) * (^agg * uint64) voption * AsyncReplyChannel<Result<'agg, string>>
        | Post of string * ('agg -> (string * ReadOnlyMemory<byte>) seq * 'agg) * AsyncReplyChannel<Result<'agg, string>>
        | Get of AsyncReplyChannel<Result<'agg, string>>

    let inline run lg reader writer agg version cmd = async {
        try return! Factory.build lg writer agg version cmd
        with
        | ApplyCommandException ex -> return failwith ex
        | ex ->
            lg.Error ex.StackTrace "Save events failed: %s" ex.Message
            let! (events: (uint64 * string * ReadOnlyMemory<byte>) seq) = reader <| version + 1uL
            let agg, version =
                Seq.fold (fun agg elem ->
                    let (_, evType, data) = elem
                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                ) agg events, version + uint64 (Seq.length events)
            return! Factory.build lg writer agg version cmd }

    let inline agent (lg: DiagnoseLog.T) reader writer aggKey shot =
        MailboxProcessor<Msg< ^agg>>.Start <| fun inbox ->
            let rec loop agg version closed = async {
                match! inbox.Receive() with
                | Init (traceId, apply, snapshot, channel) ->
                    lg.Trace "Initialize cache of [%s]" aggKey
                    try
                        let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                        let! agg', version' = Factory.init lg reader writer snapshot (apply, metadata, channel)
                        return! loop agg' version' (^agg : (member Closed : bool) agg)
                    with ex ->
                        lg.Error ex.StackTrace "Initialize cache of [%s] failed: %s" aggKey ex.Message
                        Error ex.Message |> Factory.reply channel |> Async.Start
                | Post (traceId, apply, channel) ->
                    lg.Trace "Apply command to [%s]" aggKey
                    if closed then
                        lg.Warn "Aggregate closed."
                        Error "Aggregate closed." |> Factory.reply channel |> Async.Start
                    else
                        try
                            let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                            let! agg', len = run lg reader writer agg version (apply, metadata, channel)
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
            loop (^agg : (static member Initial : ^agg) ()) 0uL false

    let inline post (agent: MailboxProcessor<Msg< ^agg>>) traceId apply channel =
        agent.Post <| Post (traceId, apply, channel)


    let inline get (agent: MailboxProcessor<Msg< ^agg>>) channel =
        agent.Post <| Get channel


module Batched =

    type Msg<'agg> =
        | Init of string * ('agg -> (string * ReadOnlyMemory<byte>) seq * 'agg) * (^agg * uint64) voption * AsyncReplyChannel<Result<'agg, string>>
        | Add of string * ('agg -> (string * ReadOnlyMemory<byte>) seq * 'agg) * AsyncReplyChannel<Result<'agg, string>>
        | Launch of Timer
        | Get of AsyncReplyChannel<Result<'agg, string>>

    let inline run lg reader writer agg version cmd = async {
        try return! Factory.batch lg writer agg version cmd
        with ex ->
            lg.Error ex.StackTrace "Save events failed: %s" ex.Message
            let! (events: (uint64 * string * ReadOnlyMemory<byte>) seq) = reader <| version + 1uL
            let agg, version =
                Seq.fold (fun agg elem ->
                    let (_, evType, data) = elem
                    (^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg)) agg) evType data
                ) agg events, version + uint64 (Seq.length events)
            return! Factory.batch lg writer agg version cmd }

    let inline agent (lg: DiagnoseLog.T) reader writer interval aggKey shot =
        let agent = new MailboxProcessor<Msg< ^agg>> (fun inbox ->
            let rec loop agg version closed batch = async {
                match! inbox.Receive() with
                | Init (traceId, apply, snapshot, channel) ->
                    lg.Trace "Initialize cache of [%s]" aggKey
                    try
                        let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                        let! agg', version' = Factory.init lg reader writer snapshot (apply, metadata, channel)
                        return! loop agg' version' (^agg : (member Closed : bool) agg) []
                    with ex ->
                        lg.Error ex.StackTrace "Initialize cache of [%s] failed: %s" aggKey ex.Message
                        Error ex.Message |> Factory.reply channel |> Async.Start
                | Add (traceId, apply, channel) ->
                    lg.Trace "Add command for [%s]" aggKey
                    if closed then
                        lg.Warn "Aggregate closed."
                        Error "Aggregate closed." |> Factory.reply channel |> Async.Start
                    else
                        let metadata = Encoding.ASCII.GetBytes ("{\"TraceId\":\"" + traceId + "\"}") |> ReadOnlyMemory |> Nullable
                        return! loop agg version closed <| (apply, metadata, channel) :: batch
                | Launch timer ->
                    lg.Trace "Batch apply commands to [%s]" aggKey
                    if not batch.IsEmpty then
                        try
                            let! agg', len = run lg reader writer agg version batch
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
                            |> List.map (fun (_, _, channel) -> Factory.reply channel <| Error ex.Message)
                            |> Async.Parallel |> Async.RunSynchronously |> ignore
                            return! loop agg version closed []
                | Get channel -> Ok agg |> Factory.reply channel |> Async.Start
                return! loop agg version closed batch }
            loop (^agg : (static member Initial : ^agg) ()) UInt64.MaxValue false [])
        agent.Start()
        Async.Start <| async {
            let timer = new Timer (interval)
            timer.AutoReset <- true
            timer.Elapsed.Add (fun _ -> agent.Post <| Launch timer )
            timer.Start() }
        agent

    let inline post (agent: MailboxProcessor<Msg< ^agg>>) traceId apply channel =
        agent.Post <| Add (traceId, apply, channel)

    let inline get (agent: MailboxProcessor<Msg< ^agg>>) channel =
        agent.Post <| Get channel


module Observed =

    type Msg<'agg> =
        | Init of (^agg * uint64) voption
        | Append of uint64 * string * ReadOnlyMemory<byte>
        | Get of AsyncReplyChannel<Result<'agg, string>>

    let inline agent (lg: DiagnoseLog.T) reader aggKey shot =
        MailboxProcessor<Msg< ^agg>>.Start <| fun inbox ->
            let rec loop agg version = async {
                match! inbox.Receive() with
                | Init snapshot ->
                    lg.Trace "Initialize cache of [%s]" aggKey
                    try
                        let! agg', version' = Factory.raw lg reader snapshot
                        return! loop agg' version'
                    with ex ->
                        lg.Error ex.StackTrace "Initialize cache of [%s] failed: %s" aggKey ex.Message
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
                            let! events = reader <| version + 1uL
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
                | Get channel ->
                    Ok agg |> Factory.reply channel |> Async.Start
                return! loop agg version }
            loop (^agg : (static member Initial : ^agg) ()) UInt64.MaxValue

    let inline append (agent: MailboxProcessor<Msg< ^agg>>) number evType data =
        agent.Post <| Append (number, evType, data)

    let inline get (agent: MailboxProcessor<Msg< ^agg>>) channel =
        agent.Post <| Get channel