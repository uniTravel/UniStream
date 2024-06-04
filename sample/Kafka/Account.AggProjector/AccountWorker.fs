namespace Account.AggProjector

open System.Threading
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open UniStream.Domain
open Account.Domain


type AccountWorker([<FromKeyedServices(typeof<Account>)>] projector: IWorker) =
    inherit BackgroundService()

    override _.ExecuteAsync(ct: CancellationToken) = projector.Launch ct
