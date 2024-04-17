namespace Account.Init

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
        manager.EnableAsync("$by_event_type").Wait()

        let settings =
            PersistentSubscriptionSettings(true, consumerStrategyName = SystemConsumerStrategies.Pinned)

        sub
            .CreateToStreamAsync("$et-Account.Domain.InitPeriod", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.OpenPeriod", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.SetLimit", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.ChangeLimit", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.SetTransLimit", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.Deposit", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.Withdraw", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.TransferOut", "account", settings)
            .Wait()

        sub
            .CreateToStreamAsync("$et-Account.Domain.TransferIn", "account", settings)
            .Wait()

        0 // exit code
