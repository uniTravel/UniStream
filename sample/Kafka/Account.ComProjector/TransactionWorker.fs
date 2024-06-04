namespace Account.ComProjector

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open UniStream.Domain
open Account.Domain


type TransactionWorker([<FromKeyedServices(typeof<Transaction>)>] projector: IWorker) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) = projector.Launch ct
