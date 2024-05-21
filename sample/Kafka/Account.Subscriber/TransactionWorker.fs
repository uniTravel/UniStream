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
        [<FromKeyedServices(typeof<Transaction>)>] subscriber: ISubscriber,
        [<FromKeyedServices(Cons.Com)>] producer: IProducer<string, byte array>,
        svc: TransactionService
    ) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger producer svc.InitPeriod
        Handler.register subscriber logger producer svc.OpenPeriod
        Handler.register subscriber logger producer svc.SetLimit
        Handler.register subscriber logger producer svc.ChangeLimit
        Handler.register subscriber logger producer svc.SetTransLimit
        Handler.register subscriber logger producer svc.Deposit
        Handler.register subscriber logger producer svc.Withdraw
        Handler.register subscriber logger producer svc.TransferOut
        Handler.register subscriber logger producer svc.TransferIn
        subscriber.Launch ct
