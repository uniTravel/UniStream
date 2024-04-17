namespace Account.Subscriber

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application


module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddHostedService<OpenPeriodWorker>() |> ignore
        builder.Services.AddHostedService<SetLimitWorker>() |> ignore

        builder.Services.AddSubscriber(builder.Configuration)
        builder.Services.AddAggregate<Transaction>(builder.Configuration)
        builder.Services.AddSingleton<TransactionService>() |> ignore

        builder.Build().Run()

        exitCode
