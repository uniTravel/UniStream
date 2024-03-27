namespace Account.Worker

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
        builder.Services.AddHostedService<Worker>() |> ignore

        builder.Services.AddEventStore(builder.Configuration, true)
        builder.Services.AddAggregate<Account>(builder.Configuration)
        builder.Services.AddSingleton<AccountService>() |> ignore

        builder.Build().Run()

        exitCode
