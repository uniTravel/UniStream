namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type TransactionWorker(logger: ILogger<TransactionWorker>, client: IClient, sub: ISubscriber, svc: TransactionService) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Worker.run<Transaction> ct logger sub Cons.Group
        <| [ Worker.register logger client svc.InitPeriod
             Worker.register logger client svc.OpenPeriod
             Worker.register logger client svc.SetLimit
             Worker.register logger client svc.ChangeLimit
             Worker.register logger client svc.SetTransLimit
             Worker.register logger client svc.Deposit
             Worker.register logger client svc.Withdraw
             Worker.register logger client svc.TransferOut
             Worker.register logger client svc.TransferIn ]
