namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type TransactionWorker
    (
        logger: ILogger<TransactionWorker>,
        client: IClient,
        [<FromKeyedServices(typeof<Transaction>)>] subscriber: ISubscriber,
        svc: TransactionService
    ) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger client svc.InitPeriod
        Handler.register subscriber logger client svc.OpenPeriod
        Handler.register subscriber logger client svc.SetLimit
        Handler.register subscriber logger client svc.ChangeLimit
        Handler.register subscriber logger client svc.SetTransLimit
        Handler.register subscriber logger client svc.Deposit
        Handler.register subscriber logger client svc.Withdraw
        Handler.register subscriber logger client svc.TransferOut
        Handler.register subscriber logger client svc.TransferIn
        subscriber.Launch ct
