namespace UniStream.Domain

open System.Threading
open System.Collections.Generic
open Microsoft.Extensions.Logging
open EventStore.Client


type ISubscriber =
    inherit IWorker
    abstract member AddHandler: key: string -> handler: MailboxProcessor<Uuid * EventRecord> -> unit


[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate>(logger: ILogger<Subscriber<'agg>>, sub: IPersistent) =
    let sub = sub.Subscriber
    let aggType = typeof<'agg>.FullName
    let dic = Dictionary<string, MailboxProcessor<Uuid * EventRecord>>()
    let group = "UniStream"

    let subscribe (ct: CancellationToken) =
        task {
            let sub = sub.SubscribeToStream(aggType, group, cancellationToken = ct)
            let e = sub.Messages.GetAsyncEnumerator()

            while! e.MoveNextAsync() do
                match e.Current with
                | :? PersistentSubscriptionMessage.Event as ev ->
                    let comType = ev.ResolvedEvent.Event.EventType
                    dic[comType].Post(ev.ResolvedEvent.Event.EventId, ev.ResolvedEvent.Event)
                    sub.Ack(ev.ResolvedEvent) |> ignore
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation($"Subscription {confirm.SubscriptionId} for {aggType} started")
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError("Stream was not found")
                | _ -> logger.LogError("Unknown error")
        }

    interface ISubscriber with

        member _.AddHandler (key: string) (handler: MailboxProcessor<Uuid * EventRecord>) = dic.Add(key, handler)

        member _.Launch(ct: CancellationToken) =
            task {
                Async.Start(subscribe ct |> Async.AwaitTask, ct)
                logger.LogInformation($"Subscribe {aggType} started")
            }
