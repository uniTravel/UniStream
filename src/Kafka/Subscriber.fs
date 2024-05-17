namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Threading
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open UniStream.Domain


type Subscriber<'agg when 'agg :> Aggregate>(logger: ILogger<Subscriber<'agg>>, consumer: IConsumer<Guid, byte array>) =
    let c = consumer.Client
    let aggType = typeof<'agg>.FullName
    let topic = aggType + ":>"
    let dic = Dictionary<string, MailboxProcessor<Guid * Guid * int * byte array>>()

    let work (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    let comId = Guid(cr.Message.Headers.GetLastBytes("comId"))
                    let comType = Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("comType"))
                    let partition = BitConverter.ToInt32(cr.Message.Headers.GetLastBytes("partition"))

                    try
                        dic[comType].Post(cr.Message.Key, comId, partition, cr.Message.Value)
                    with ex ->
                        logger.LogCritical($"{ex}")
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    member _.AddHandler (key: string) (handler: MailboxProcessor<Guid * Guid * int * byte array>) =
        dic.Add(key, handler)

    interface IWorker with
        member _.Launch(ct: CancellationToken) =
            task {
                c.Subscribe(topic)
                Async.Start(work ct, ct)
                logger.LogInformation($"Subscribe {aggType} started")
            }
