namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type TransactionWorker
    (
        logger: ILogger<TransactionWorker>,
        subscriber: ISubscriber<Transaction>,
        producer: IProducer<Transaction>,
        svc: TransactionService
    ) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger producer ct svc.InitPeriod
        Handler.register subscriber logger producer ct svc.OpenPeriod
        Handler.register subscriber logger producer ct svc.SetLimit
        Handler.register subscriber logger producer ct svc.ChangeLimit
        Handler.register subscriber logger producer ct svc.SetTransLimit
        Handler.register subscriber logger producer ct svc.Deposit
        Handler.register subscriber logger producer ct svc.Withdraw
        Handler.register subscriber logger producer ct svc.TransferOut
        Handler.register subscriber logger producer ct svc.TransferIn
        subscriber.Launch ct
