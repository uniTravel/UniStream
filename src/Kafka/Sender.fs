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


type ISender =

    abstract member Agent: MailboxProcessor<string * Message<string, byte array> * AsyncReplyChannel<Result<unit, exn>>>


type Hub =
    | Add of string * AsyncReplyChannel<Result<unit, exn>>
    | Reply of ConsumeResult<string, byte array>


type Sender<'agg when 'agg :> Aggregate>
    (logger: ILogger<Sender<'agg>>, producer: IProducer<string, byte array>, consumer: IConsumer<string, byte array>) =
    let p = producer.Client
    let c = consumer.Client
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
                    | Reply(cr) ->
                        match cr.Message.Key with
                        | comId when dic.ContainsKey comId ->
                            match cr.Message.Value with
                            | [||] ->
                                logger.LogInformation($"{comId} of {aggType} finished")
                                dic[comId].Reply <| Ok()
                            | v ->
                                let err = BitConverter.ToString v
                                logger.LogError($"{comId} of {aggType} failed: {err}")
                                dic[comId].Reply <| Error(failwith $"Apply command failed: {err}")

                            dic.Remove comId |> ignore
                        | _ -> ()

                    return! loop dic
                }

            loop <| Dictionary<string, AsyncReplyChannel<Result<unit, exn>>>())

    let consumer (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    receiver.Post <| Reply cr
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    let agent =
        new MailboxProcessor<string * Message<string, byte array> * AsyncReplyChannel<Result<unit, exn>>>(fun inbox ->
            let topic = aggType + "_Post"

            let rec loop () =
                async {
                    let! comId, msg, channel = inbox.Receive()

                    try
                        p.Produce(topic, msg)
                        receiver.Post <| Add(comId, channel)
                    with ex ->
                        channel.Reply <| Error ex

                    return! loop ()
                }

            loop ())

    do
        receiver.Start()
        agent.Start()
        c.Subscribe(topic)
        Async.Start(consumer cts.Token, cts.Token)

        while c.Assignment.Count = 0 do
            Thread.Sleep 200

    interface ISender with
        member val Agent = agent

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                cts.Cancel()
                agent.Dispose()
                receiver.Dispose()
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
            comId, msg, channel

        async {
            let comType = typeof<'com>.FullName
            let comData = JsonSerializer.SerializeToUtf8Bytes com

            match! sender.Agent.PostAndAsyncReply <| fun channel -> setup comType comData channel with
            | Ok() -> return ()
            | Error ex -> return raise ex
        }
