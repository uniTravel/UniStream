namespace Account.Subscriber

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application


module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder args

        builder.Services.AddSubscriber builder.Configuration |> ignore

        builder.Services
            .AddSubscriber<Account>(builder.Configuration)
            .AddHostedService<AccountWorker>()
            .AddSingleton<AccountService>()
        |> ignore

        builder.Services
            .AddSubscriber<Transaction>(builder.Configuration)
            .AddHostedService<TransactionWorker>()
            .AddSingleton<TransactionService>()
        |> ignore

        builder.Build().Run()

        0 // exit code
