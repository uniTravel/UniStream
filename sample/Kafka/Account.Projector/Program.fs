namespace Account.Projector

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain


module Program =

    [<EntryPoint>]
    let main args =

        let builder = Host.CreateApplicationBuilder(args)

        builder.Services.AddProjector(builder.Configuration) |> ignore

        builder.Services
            .AddHostedService<AccountWorker>()
            .AddSingleton<IWorker<Account>, Projector<Account>>()
        |> ignore

        builder.Services
            .AddHostedService<TransactionWorker>()
            .AddSingleton<IWorker<Transaction>, Projector<Transaction>>()
        |> ignore

        builder.Build().Run()

        0 // exit code
