namespace Account.Projector

open System.Threading
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain


type TransactionWorker(projector: IWorker<Transaction>) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) = projector.Launch ct
