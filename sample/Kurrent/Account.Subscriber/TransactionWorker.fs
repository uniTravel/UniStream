namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type TransactionWorker
    (logger: ILogger<TransactionWorker>, client: IClient, subscriber: ISubscriber<Transaction>, svc: TransactionService)
    =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger client ct svc.InitPeriod
        Handler.register subscriber logger client ct svc.OpenPeriod
        Handler.register subscriber logger client ct svc.SetLimit
        Handler.register subscriber logger client ct svc.ChangeLimit
        Handler.register subscriber logger client ct svc.SetTransLimit
        Handler.register subscriber logger client ct svc.Deposit
        Handler.register subscriber logger client ct svc.Withdraw
        Handler.register subscriber logger client ct svc.TransferOut
        Handler.register subscriber logger client ct svc.TransferIn
        subscriber.Launch ct
