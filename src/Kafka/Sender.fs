namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Confluent.Kafka
open UniStream.Domain


type Backlog =
    | Add of Guid * AsyncReplyChannel<Result<unit, exn>>
    | Remove of Guid * Result<unit, exn>

[<Sealed>]
type Sender<'agg when 'agg :> Aggregate>
    (logger: ILogger<Sender<'agg>>, options: IOptionsMonitor<CommandOptions>, cp: IProducer<'agg>, tc: IConsumer<'agg>)
    =
    let cp = cp.Client
    let tc = tc.Client
    let options = options.Get(typeof<'agg>.Name)
    let interval = options.Interval * 60000
    let aggType = typeof<'agg>.FullName
    let topic = aggType + "_Command"
    let cts = new CancellationTokenSource()
    let mutable dispose = false

    let todo =
        (fun (inbox: MailboxProcessor<Backlog>) ->
            let rec loop (todo: Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>) =
                async {
                    match! inbox.Receive() with
                    | Add(comId, channel) ->
                        if todo.ContainsKey comId then
                            todo[comId] <- channel
                        else
                            todo.Add(comId, channel)
                    | Remove(comId, result) ->
                        if todo.ContainsKey comId then
                            todo[comId].Reply result
                            logger.LogInformation $"Apply [{comId}] of {aggType} completed"
                            todo.Remove comId |> ignore

                    return! loop todo
                }

            loop (Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>())
         , cts.Token)
        |> MailboxProcessor<Backlog>.Start

    let delivery (aggId: Guid) (comType: string) (comId: Guid) (report: DeliveryReport<byte array, byte array>) =
        match report.Error.Code with
        | ErrorCode.NoError -> logger.LogInformation $"Send {comType}[{comId}] of {aggType}[{aggId}] success"
        | err -> failwith <| err.GetReason()

    let agent =
        (fun (inbox: MailboxProcessor<Msg>) ->
            let rec loop (cache: Dictionary<Guid, DateTime * Result<unit, exn>>) =
                async {
                    match! inbox.Receive() with
                    | Send(aggId, comId, comType, comData, channel) ->
                        todo.Post <| Add(comId, channel)

                        if cache.ContainsKey comId then
                            todo.Post <| Remove(comId, snd cache[comId])
                        else
                            try
                                let aId = aggId.ToByteArray()
                                let h = Headers()
                                let msg = Message<byte array, byte array>(Key = aId, Value = comData, Headers = h)
                                msg.Headers.Add("comId", comId.ToByteArray())
                                msg.Headers.Add("comType", Encoding.ASCII.GetBytes comType)
                                cp.Produce(topic, msg, delivery aggId comType comId)
                            with ex ->
                                let ex = WriteException("Send command failed", ex)
                                logger.LogError(ex, $"Send {comType}[{comId}] of {aggType}[{aggId}] failed")
                                todo.Post <| Remove(comId, Core.Error ex)
                    | Receive(comId, result) ->
                        todo.Post <| Remove(comId, result)
                        let expire = DateTime.UtcNow.AddMilliseconds interval
                        cache.Add(comId, (expire, result)) |> ignore
                    | Refresh now ->
                        cache
                        |> Seq.iter (fun (KeyValue(comId, (expire, _))) ->
                            if expire < now then
                                cache.Remove comId |> ignore)

                    return! loop cache
                }

            loop (Dictionary<Guid, DateTime * Result<unit, exn>>())
         , cts.Token)
        |> MailboxProcessor<Msg>.Start

    let consumer =
        async {
            while true do
                try
                    match tc.Consume 10000 with
                    | null -> ()
                    | cr ->
                        let comId = Guid(cr.Message.Headers.GetLastBytes "comId")

                        match Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes "evtType") with
                        | "Fail" ->
                            let err = JsonSerializer.Deserialize cr.Message.Value
                            let ex = Exception $"Handle command failed: {err}"
                            agent.Post <| Receive(comId, Core.Error ex)
                        | _ -> agent.Post <| Receive(comId, Ok())
                with ex ->
                    logger.LogError(ex, $"Apply command error")
        }

    let refresh =
        async {
            do! Async.Sleep interval
            agent.Post <| Refresh DateTime.UtcNow
        }

    do
        tc.Subscribe aggType
        Async.Start(refresh, cts.Token)
        Async.Start(consumer, cts.Token)
        logger.LogInformation $"Subscription for {aggType} started"

    interface ISender<'agg> with
        member val send =
            fun aggId comId comType comData ->
                agent.PostAndAsyncReply(fun channel -> Send(aggId, comId, comType, comData, channel))

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                todo.Dispose()
                dispose <- true
