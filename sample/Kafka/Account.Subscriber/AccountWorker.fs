namespace Account.Subscriber

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open UniStream.Domain
open Account.Domain
open Account.Application


type AccountWorker
    (logger: ILogger<AccountWorker>, subscriber: ISubscriber<Account>, producer: IProducer<Account>, svc: AccountService)
    =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) =
        Handler.register subscriber logger producer svc.CreateAccount
        Handler.register subscriber logger producer svc.VerifyAccount
        Handler.register subscriber logger producer svc.ApproveAccount
        Handler.register subscriber logger producer svc.LimitAccount
        subscriber.Launch ct
