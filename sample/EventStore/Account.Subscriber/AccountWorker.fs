namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type AccountWorker
    (logger: ILogger<AccountWorker>, client: IClient, subscriber: ISubscriber<Account>, svc: AccountService) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger client svc.CreateAccount
        Handler.register subscriber logger client svc.VerifyAccount
        Handler.register subscriber logger client svc.ApproveAccount
        Handler.register subscriber logger client svc.LimitAccount
        subscriber.Launch ct
