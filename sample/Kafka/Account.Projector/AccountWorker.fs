namespace Account.Projector

open System.Threading
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain


type AccountWorker(projector: IWorker<Account>) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) = projector.Launch ct
