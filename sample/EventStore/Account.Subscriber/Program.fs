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
        builder.Services.AddHostedService<InitPeriodWorker>() |> ignore
        builder.Services.AddHostedService<OpenPeriodWorker>() |> ignore
        builder.Services.AddHostedService<SetLimitWorker>() |> ignore
        builder.Services.AddHostedService<ChangeLimitWorker>() |> ignore
        builder.Services.AddHostedService<SetTransLimitWorker>() |> ignore
        builder.Services.AddHostedService<DepositWorker>() |> ignore
        builder.Services.AddHostedService<WithdrawWorker>() |> ignore
        builder.Services.AddHostedService<TransferOutWorker>() |> ignore
        builder.Services.AddHostedService<TransferInWorker>() |> ignore

        builder.Services.AddSubscriber(builder.Configuration)
        builder.Services.AddAggregate<Transaction>(builder.Configuration)
        builder.Services.AddSingleton<TransactionService>() |> ignore

        builder.Build().Run()

        exitCode
