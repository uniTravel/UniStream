namespace Account.Initializer

open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open EventStore.Client
open UniStream.Domain

module Program =

    [<EntryPoint>]
    let main args =
        let builder = Host.CreateApplicationBuilder(args)
        builder.Services.AddInitializer(builder.Configuration)

        use host = builder.Build()
        use serviceScope = host.Services.CreateScope()
        let services = serviceScope.ServiceProvider
        use sub = services.GetRequiredService<ISubscriber>().Subscriber
        use manager = services.GetRequiredService<IManager>().Manager

        manager.EnableAsync("$by_correlation_id").Wait()

        let settings =
            PersistentSubscriptionSettings(true, consumerStrategyName = SystemConsumerStrategies.Pinned)

        sub
            .CreateToStreamAsync("Account.Domain.Transaction", "account", settings)
            .Wait()

        0 // exit code
