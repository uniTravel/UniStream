namespace Account.Worker

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open EventStore.Client
open FSharp.Control
open UniStream.Domain
open Account.Application


type Worker(logger: ILogger<Worker>, svc: AccountService, sub: ISubscriber) =
    inherit BackgroundService()
    let sub = sub.Subscriber

    override _.ExecuteAsync(ct: CancellationToken) =
        task {
            use subscription =
                sub.SubscribeToStream("$ce-Account.Domain.Account", "group", cancellationToken = ct)

            do!
                subscription.Messages
                |> AsyncSeq.ofAsyncEnum
                |> AsyncSeq.iter (fun x ->
                    match x with
                    | :? PersistentSubscriptionMessage.Event as ev ->
                        logger.LogInformation(
                            $"{ev.ResolvedEvent.Event.EventStreamId}, {ev.ResolvedEvent.Event.EventNumber}"
                        )

                        subscription.Ack(ev.ResolvedEvent) |> ignore
                    | :? PersistentSubscriptionMessage.SubscriptionConfirmation as confirm ->
                        logger.LogInformation($"Subscription {confirm.SubscriptionId} started")
                    | :? PersistentSubscriptionMessage.NotFound -> logger.LogWarning("NotFound")
                    | :? PersistentSubscriptionMessage.Unknown -> logger.LogWarning("Unknown")
                    | _ -> logger.LogCritical("Error message type"))
        }
