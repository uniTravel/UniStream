namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Threading
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open UniStream.Domain


[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate>(logger: ILogger<Subscriber<'agg>>, cc: IConsumer) =
    let cc = cc.Client
    let aggType = typeof<'agg>.FullName
    let dic = Dictionary<string, MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>>()

    let work (ct: CancellationToken) =
        async {
            try
                while true do
                    let cr = cc.Consume ct
                    let comId = Guid(cr.Message.Headers.GetLastBytes("comId"))
                    let comType = Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes("comType"))

                    try
                        dic[comType].Post(Guid(cr.Message.Key), comId, ReadOnlyMemory cr.Message.Value)
                    with ex ->
                        logger.LogCritical($"{ex}")
            with ex ->
                logger.LogError($"Consume loop breaked: {ex}")
        }

    interface ISubscriber<'agg> with

        member _.AddHandler (key: string) (handler: MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>) =
            dic.Add(key, handler)

        member _.Launch(ct: CancellationToken) =
            task {
                cc.Subscribe(aggType + "_Command")
                Async.Start(work ct, ct)
                logger.LogInformation($"Subscription of command for {aggType} started")
            }
