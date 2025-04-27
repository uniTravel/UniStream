namespace UniStream.Domain

open System
open System.Collections.Generic
open System.Text
open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open UniStream.Domain
open Confluent.Kafka


[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate>
    (logger: ILogger<Subscriber<'agg>>, lifetime: IHostApplicationLifetime, cc: IConsumer<'agg>) =
    let cc = cc.Client
    let aggType = typeof<'agg>.FullName
    let dic = Dictionary<string, MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>>()
    let mutable dispose = false

    interface ISubscriber<'agg> with

        member _.AddHandler (key: string) (handler: MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>) =
            dic.Add(key, handler)

        member _.Launch(ct: CancellationToken) =
            task {
                cc.Subscribe(aggType + "_Command")
                logger.LogInformation $"Subscription of command for {aggType} started"
                do! Tasks.Task.Delay 10

                while not ct.IsCancellationRequested do
                    try
                        match cc.Consume ct with
                        | null -> ()
                        | cr ->
                            let aggId = Guid cr.Message.Key
                            let comId = Guid(cr.Message.Headers.GetLastBytes "comId")
                            let comType = Encoding.ASCII.GetString(cr.Message.Headers.GetLastBytes "comType")
                            logger.LogInformation $"Receive {comType}[{comId}] of {aggType}[{aggId}] success"

                            if dic.ContainsKey comType then
                                dic[comType].Post(aggId, comId, ReadOnlyMemory cr.Message.Value)
                            else
                                raise <| RegisterException $"Handler of {comType} not registered"
                    with
                    | :? ConsumeException as ex ->
                        let err = ex.Error.Reason
                        logger.LogError(ex, $"Receive command error: {err}")
                    | :? RegisterException as ex ->
                        logger.LogCritical(ex, "Handler register error")
                        lifetime.StopApplication()
                    | ex ->
                        logger.LogError(ex, "Unknown error")
                        lifetime.StopApplication()
            }

    interface IDisposable with
        member _.Dispose() =
            if not dispose then
                dic.Values |> Seq.iter (fun agent -> agent.Dispose())
                dispose <- true
