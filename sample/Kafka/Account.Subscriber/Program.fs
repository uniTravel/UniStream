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
        builder.Services.AddHostedService<TransactionWorker>() |> ignore

        builder.Services.AddSubscriber(builder.Configuration)
        builder.Services.AddAggregate<Transaction>(builder.Configuration)
        builder.Services.AddSingleton<TransactionService>() |> ignore

        builder.Services.AddSingleton<ISubscriber<Transaction>, Subscriber<Transaction>>()
        |> ignore

        builder.Services.AddSingleton<IStream<Transaction>, Stream<Transaction>>()
        |> ignore

        let app = builder.Build()

        using (app.Services.CreateScope()) (fun scope ->
            let services = scope.ServiceProvider
            services.GetRequiredService<TransactionService>() |> ignore)

        app.Run()

        0 // exit code
