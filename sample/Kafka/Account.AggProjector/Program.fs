namespace Account.AggProjector

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain


module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddProjector(builder.Configuration)
        builder.Services.AddHostedService<AccountWorker>() |> ignore
        builder.Services.AddHostedService<TransactionWorker>() |> ignore

        builder.Services.AddKeyedSingleton<IWorker, AggProjector<Account>>(typeof<Account>)
        |> ignore

        builder.Services.AddKeyedSingleton<IWorker, AggProjector<Transaction>>(typeof<Transaction>)
        |> ignore

        builder.Build().Run()

        0 // exit code
