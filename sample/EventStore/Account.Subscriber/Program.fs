namespace Account.Subscriber

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open UniStream.Domain
open Account.Domain
open Account.Application


module Program =
    [<EntryPoint>]
    let main args =

        let builder = Host.CreateApplicationBuilder(args)

        builder.Services.AddSubscriber(builder.Configuration) |> ignore

        builder.Services
            .AddSubscriber<Transaction>(builder.Configuration)
            .AddHostedService<TransactionWorker>()
            .AddSingleton<TransactionService>()
        |> ignore

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<TransactionService>() |> ignore)

        app.Run()

        0 // exit code
