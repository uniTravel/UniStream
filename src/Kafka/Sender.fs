namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Text.Json
open System.Threading
open Microsoft.Extensions.Logging
open Confluent.Kafka
open Microsoft.FSharp.Core
open UniStream.Domain


type Hub =
    | Add of string * AsyncReplyChannel<Result<unit, exn>>
    | Reply of string * Result<unit, exn>


type Sender<'agg when 'agg :> Aggregate>
    (logger: ILogger<Sender<'agg>>, producer: IProducer<string, byte array>, consumer: IConsumer<string, byte array>) =
    let p = producer.Client
    let c = consumer.Client
    // TODO 分区需要从配置读取
    let partition = 0
    let aggType = typeof<'agg>.FullName
    let cts = new CancellationTokenSource()
    let topic = aggType + "_Reply"
    let mutable dispose = false

    let receiver =
        new MailboxProcessor<Hub>(fun inbox ->
            let rec loop (dic: Dictionary<string, AsyncReplyChannel<Result<unit, exn>>>) =
                async {
                    match! inbox.Receive() with
                    | Add(comId, channel) -> dic.Add(comId, channel)
                    | Reply(comId, result) ->
                        dic[comId].Reply result
                        dic.Remove(comId) |> ignore

                    return! loop dic
                }

            loop <| Dictionary<string, AsyncReplyChannel<Result<unit, exn>>>())

    let agent =
        new MailboxProcessor<string * Message<string, byte array> * AsyncReplyChannel<Result<unit, exn>>>(fun inbox ->
            let topic = aggType + "_Post"

            let rec loop () =
                async {
                    let! comId, msg, channel = inbox.Receive()

                    try
                        p.Produce(topic, msg)
                        p.Flush(TimeSpan.FromSeconds(10)) |> ignore
                        receiver.Post <| Add(comId, channel)
                    with ex ->
                        channel.Reply <| Error ex

                    return! loop ()
                }

            loop ())

    let consumer (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    let comId = cr.Message.Key

                    match BitConverter.ToString cr.Message.Value with
                    | "" -> receiver.Post <| Reply(comId, Ok())
                    | err -> receiver.Post <| Reply(comId, Error(failwith $"Apply command failed: {err}"))
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    do
        receiver.Start()
        agent.Start()
        c.Subscribe(topic)
        c.Assign [ TopicPartition(topic, Partition partition) ]
        Async.Start(consumer cts.Token, cts.Token)

    member val Partition = partition

    member val Agent = agent

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                receiver.Dispose()
                dispose <- true


module Sender =

    let inline send<'agg, 'com, 'evt when Com<'agg, 'com, 'evt>> (sender: Sender<'agg>) (aggId: Guid) (com: 'com) =
        let setup (aggId: Guid) (com: 'com) (channel: AsyncReplyChannel<Result<unit, exn>>) =
            let aggId = aggId.ToString()
            let comId = Guid.NewGuid().ToString()
            let comType = typeof<'com>.FullName
            let comData = JsonSerializer.SerializeToUtf8Bytes com
            let h = Headers()
            let msg = Message<string, byte array>(Key = aggId, Value = comData, Headers = h)
            msg.Headers.Add("comId", Encoding.ASCII.GetBytes comId)
            msg.Headers.Add("comType", Encoding.ASCII.GetBytes comType)
            msg.Headers.Add("partition", BitConverter.GetBytes sender.Partition)
            comId, msg, channel

        async {
            match! sender.Agent.PostAndAsyncReply <| fun channel -> setup aggId com channel with
            | Ok() -> return ()
            | Error ex -> return raise ex
        }
