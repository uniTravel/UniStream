namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
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
        admin: IAdmin,
        cp: IProducer,
        tc: IConsumer
    ) =
    let cp = cp.Client
    let tc = tc.Client
    let admin = admin.Client
    let options = options.Get(typeof<'agg>.Name)
    let interval = options.Interval * 1000
    let aggType = typeof<'agg>.FullName
    let topic = aggType + "_Command"
    let cts = new CancellationTokenSource()
    let mutable dispose = false
    let todo = Dictionary<Guid, AsyncReplyChannel<Result<unit, exn>>>()
    let cache = Dictionary<Guid, DateTime * (Guid -> unit)>()

    let reply (cr: ConsumeResult<byte array, byte array>) comId =
        match Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("evtType")) with
        | "Fail" ->
            let err = BitConverter.ToString cr.Message.Value
            logger.LogError($"{comId} of {aggType} failed: {err}")
            todo[comId].Reply <| Error(failwith $"Apply command failed: {err}")
        | _ ->
            logger.LogInformation($"{comId} of {aggType} finished")
            todo[comId].Reply <| Ok()

    let agent =
        new MailboxProcessor<Msg>(fun inbox ->
            let rec loop () =
                async {
                    match! inbox.Receive() with
                    | Send(aggId, comId, comType, comData, channel) ->
                        match comId with
                        | _ when cache.ContainsKey comId ->
                            let _, reply = cache[comId]
                            reply comId
                            cache.Remove comId |> ignore
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
                    | Receive(comId, reply) ->
                        match comId with
                        | _ when todo.ContainsKey comId ->
                            reply comId
                            todo.Remove comId |> ignore
                        | _ when not <| cache.ContainsKey comId ->
                            let expire = DateTime.UtcNow.AddMilliseconds interval
                            cache.Add(comId, (expire, reply)) |> ignore
                        | _ -> ()
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
                    let cr = tc.Consume ct
                    let comId = Guid(cr.Message.Headers.GetLastBytes("comId"))
                    let reply = reply cr
                    agent.Post <| Receive(comId, reply)
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    do
        agent.Start()

        admin
            .GetMetadata(TimeSpan.FromSeconds 2.0)
            .Topics.Find(fun t -> t.Topic = aggType)
            .Partitions
        |> Seq.map (fun x -> TopicPartition(aggType, x.PartitionId))
        |> tc.Assign

        Async.Start(consumer cts.Token, cts.Token)
        Async.Start(Sender.timer interval (fun _ -> agent.Post <| Refresh(DateTime.UtcNow)), cts.Token)

        while tc.Assignment.Count = 0 do
            Thread.Sleep 200

        logger.LogInformation($"Subscription for {aggType} started")

    interface ISender<'agg> with
        member val Agent = agent

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                dispose <- true
