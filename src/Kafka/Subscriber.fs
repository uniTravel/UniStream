namespace UniStream.Domain

open System.Collections.Generic
open System.Text
open System.Threading
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open UniStream.Domain


type ISubscriber =
    inherit IWorker
    abstract member AddHandler: key: string -> handler: MailboxProcessor<string * string * byte array> -> unit


type Subscriber<'agg when 'agg :> Aggregate>(logger: ILogger<Subscriber<'agg>>, consumer: IConsumer<string, byte array>)
    =
    let c = consumer.Client
    let aggType = typeof<'agg>.FullName
    let dic = Dictionary<string, MailboxProcessor<string * string * byte array>>()

    let work (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = c.Consume ct
                    let comId = Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("comId"))
                    let comType = Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("comType"))

                    try
                        dic[comType].Post(cr.Message.Key, comId, cr.Message.Value)
                    with ex ->
                        logger.LogCritical($"{ex}")
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    interface ISubscriber with

        member _.AddHandler (key: string) (handler: MailboxProcessor<string * string * byte array>) =
            dic.Add(key, handler)

        member _.Launch(ct: CancellationToken) =
            task {
                c.Subscribe(aggType + "_Post")
                Async.Start(work ct, ct)
                logger.LogInformation($"Subscribe {aggType} started")
            }
