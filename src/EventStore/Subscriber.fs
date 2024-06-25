namespace UniStream.Domain

open System
open System.Text
open System.Threading
open System.Collections.Generic
open Microsoft.Extensions.Logging
open EventStore.Client


[<Sealed>]
type Subscriber<'agg when 'agg :> Aggregate>(logger: ILogger<Subscriber<'agg>>, sub: IPersistent) =
    let sub = sub.Subscriber
    let aggType = typeof<'agg>.FullName
    let dic = Dictionary<string, MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>>()
    let group = "UniStream"

    let subscribe (ct: CancellationToken) =
        task {
            let sub = sub.SubscribeToStream(aggType, group, cancellationToken = ct)
            let e = sub.Messages.GetAsyncEnumerator()

            while! e.MoveNextAsync() do
                match e.Current with
                | :? PersistentSubscriptionMessage.Event as ev ->
                    let er = ev.ResolvedEvent
                    let comType = er.Event.EventType
                    let aggId = Guid(Encoding.ASCII.GetString(er.Event.Metadata.Span)[19..54])
                    dic[comType].Post(aggId, er.Event.EventId.ToGuid(), er.Event.Data)
                    sub.Ack(er).Wait()
                | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                    logger.LogInformation($"Subscription {confirm.SubscriptionId} for {aggType} started")
                | :? PersistentSubscriptionMessage.NotFound -> logger.LogError("Stream was not found")
                | _ -> logger.LogError("Unknown error")
        }

    interface ISubscriber<'agg> with

        member _.AddHandler (key: string) (handler: MailboxProcessor<Guid * Guid * ReadOnlyMemory<byte>>) =
            dic.Add(key, handler)

        member _.Launch(ct: CancellationToken) =
            task { Async.Start(subscribe ct |> Async.AwaitTask, ct) }
