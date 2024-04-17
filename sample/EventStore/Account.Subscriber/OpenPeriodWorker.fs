namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Application


type OpenPeriodWorker(logger: ILogger<OpenPeriodWorker>, client: IClient, sub: ISubscriber, svc: TransactionService) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Worker.run ct logger client sub Cons.Group svc.OpenPeriod
