namespace Account.Projector

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

        builder.Services.AddSingleton<IWorker<Account>, Projector<Account>>() |> ignore

        builder.Services.AddSingleton<IWorker<Transaction>, Projector<Transaction>>()
        |> ignore

        builder.Build().Run()

        0 // exit code
