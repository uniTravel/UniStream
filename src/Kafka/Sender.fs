namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Options
open Confluent.Kafka
open Microsoft.FSharp.Core
open UniStream.Domain


type Msg =
    | Send of string * Message<string, byte array> * AsyncReplyChannel<Result<unit, exn>>
    | Receive of ConsumeResult<string, byte array>
    | Refresh of DateTime


type ISender =

    abstract member Agent: MailboxProcessor<Msg>


[<Sealed>]
type Sender<'agg when 'agg :> Aggregate>
    (
        logger: ILogger<Sender<'agg>>,
        options: IOptionsMonitor<CommandOptions>,
        producer: IProducer<string, byte array>,
        consumer: IConsumer<string, byte array>
    ) =
    let p = producer.Client
    let c = consumer.Client
    let options = options.Get(typeof<'agg>.Name)
    let interval = options.Interval * 1000
    let aggType = typeof<'agg>.FullName
    let cts = new CancellationTokenSource()
    let mutable dispose = false

    let agent =
        new MailboxProcessor<Msg>(fun inbox ->
            let topic = aggType + "_Command"
            let todo = Dictionary<string, AsyncReplyChannel<Result<unit, exn>>>()
            let cache = Dictionary<string, DateTime * ConsumeResult<string, byte array>>()

            let reply comId (cr: ConsumeResult<string, byte array>) =
                match Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("evtType")) with
                | "Fail" ->
                    let err = BitConverter.ToString cr.Message.Value
                    logger.LogError($"{comId} of {aggType} failed: {err}")
                    todo[comId].Reply <| Error(failwith $"Apply command failed: {err}")
                | _ ->
                    logger.LogInformation($"{comId} of {aggType} finished")
                    todo[comId].Reply <| Ok()

            let rec loop () =
                async {
                    match! inbox.Receive() with
                    | Send(comId, msg, channel) ->
                        match comId with
                        | _ when cache.ContainsKey comId ->
                            let _, cr = cache[comId]
                            reply comId cr
                            cache.Remove comId |> ignore
                        | _ when todo.ContainsKey comId -> todo[comId] <- channel
                        | _ ->
                            try
                                p.Produce(topic, msg)
                                todo.Add(comId, channel)
                            with ex ->
                                channel.Reply <| Error ex
                    | Receive(cr) ->
                        match cr.Message.Key with
                        | comId when todo.ContainsKey comId ->
                            reply comId cr
                            todo.Remove comId |> ignore
                        | comId -> cache.Add(comId, (DateTime.UtcNow.AddMilliseconds interval, cr)) |> ignore
                    | Refresh(now) ->
                        cache
                        |> Seq.iter (fun (KeyValue(comId, (expire, _))) ->
                            if expire < now then
                                cache.Remove comId |> ignore)

                    return! loop ()
                }

            loop ())

    let consumer (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    agent.Post <| Receive cr
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    let createTimer (interval: float) work =
        let timer = new Timers.Timer(interval)
        timer.AutoReset <- true
        timer.Elapsed.Add work
        async { timer.Start() }

    do
        agent.Start()
        c.Subscribe(aggType)
        Async.Start(consumer cts.Token, cts.Token)
        Async.Start(createTimer interval (fun _ -> agent.Post <| Refresh(DateTime.UtcNow)), cts.Token)

        while c.Assignment.Count = 0 do
            Thread.Sleep 200

        logger.LogInformation($"Subscription for {aggType} started")

    interface ISender with
        member val Agent = agent

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                dispose <- true


module Sender =

    let inline send<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>>
        (sender: ISender)
        (aggId: Guid)
        (comId: Guid)
        (com: 'com)
        =
        let setup (comType: string) (comData: byte array) (channel: AsyncReplyChannel<Result<unit, exn>>) =
            let aggId = aggId.ToString()
            let comId = comId.ToString()
            let h = Headers()
            let msg = Message<string, byte array>(Key = aggId, Value = comData, Headers = h)
            msg.Headers.Add("comId", Encoding.ASCII.GetBytes comId)
            msg.Headers.Add("comType", Encoding.ASCII.GetBytes comType)
            Send(comId, msg, channel)

        async {
            let comType = typeof<'com>.FullName
            let comData = JsonSerializer.SerializeToUtf8Bytes com

            match! sender.Agent.PostAndAsyncReply <| fun channel -> setup comType comData channel with
            | Ok() -> return ()
            | Error ex -> return raise ex
        }
