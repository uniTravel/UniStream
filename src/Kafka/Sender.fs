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


[<Sealed>]
type Sender<'agg when 'agg :> Aggregate>
    (
        logger: ILogger<Sender<'agg>>,
        options: IOptionsMonitor<CommandOptions>,
        admin: IAdmin<'agg>,
        cp: IProducer<'agg>,
        tc: IConsumer<'agg>
    ) =
    let cp = cp.Client
    let tc = tc.Client
    let admin = admin.Client
    let options = options.Get(typeof<'agg>.Name)
    let interval = options.Interval * 60000
    let aggType = typeof<'agg>.FullName
    let topic = aggType + "_Command"
    let cts = new CancellationTokenSource()
    let mutable dispose = false

    let log (comId: Guid) (result: Result<unit, exn>) =
        match result with
        | Ok _ -> logger.LogInformation $"Command[{comId}] of {aggType} finished"
        | Error err -> logger.LogError $"Command[{comId}] of {aggType} failed: {err}"

    let agent =
        new MailboxProcessor<Msg>(fun inbox ->
            let rec loop
                (todo: Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>)
                (cache: Dictionary<Guid, DateTime * Result<unit, exn>>)
                =
                async {
                    match! inbox.Receive() with
                    | Send(aggId, comId, comType, comData, channel) ->
                        logger.LogInformation $"Sending {comType}[{comId}] of {aggType}[{aggId}]"

                        match comId with
                        | _ when cache.ContainsKey comId ->
                            let result = snd cache[comId]
                            channel.Reply result
                            log comId result
                        | _ when todo.ContainsKey comId -> todo[comId] <- channel
                        | _ ->
                            try
                                let aggId = aggId.ToByteArray()
                                let h = Headers()
                                let msg = Message<byte array, byte array>(Key = aggId, Value = comData, Headers = h)
                                msg.Headers.Add("comId", comId.ToByteArray())
                                msg.Headers.Add("comType", Encoding.ASCII.GetBytes comType)
                                cp.Produce(topic, msg)
                                todo.Add(comId, channel)
                            with ex ->
                                channel.Reply <| Error ex
                    | Receive(comId, result) ->
                        if todo.ContainsKey comId then
                            todo[comId].Reply result
                            todo.Remove comId |> ignore
                            log comId result

                        let expire = DateTime.UtcNow.AddMilliseconds interval
                        cache.Add(comId, (expire, result)) |> ignore
                    | Refresh now ->
                        cache
                        |> Seq.iter (fun (KeyValue(comId, (expire, _))) ->
                            if expire < now then
                                cache.Remove comId |> ignore)

                    return! loop todo cache
                }

            loop
                (Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>())
                (Dictionary<Guid, DateTime * Result<unit, exn>>()))

    let consumer (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = tc.Consume ct
                    let comId = Guid(cr.Message.Headers.GetLastBytes "comId")

                    match Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes "evtType") with
                    | "Fail" ->
                        let err = JsonSerializer.Deserialize cr.Message.Value
                        agent.Post <| Receive(comId, Error(Exception $"Apply command failed: {err}"))
                    | _ -> agent.Post <| Receive(comId, Ok())
            with ex ->
                logger.LogError $"Consume loop breaked: {ex}"
        }

    do
        agent.Start()

        admin.GetMetadata(TimeSpan.FromSeconds 2.0).Topics.Find(fun t -> t.Topic = aggType).Partitions
        |> Seq.map (fun x -> TopicPartition(aggType, x.PartitionId))
        |> tc.Assign

        Async.Start(consumer cts.Token, cts.Token)
        Async.Start(Sender.timer interval (fun _ -> agent.Post <| Refresh DateTime.UtcNow), cts.Token)

        while tc.Assignment.Count = 0 do
            Thread.Sleep 200

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
                dispose <- true
