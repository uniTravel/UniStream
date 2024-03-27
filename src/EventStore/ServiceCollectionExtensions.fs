namespace UniStream.Domain

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection


[<Sealed>]
[<Extension>]
type ServiceCollectionExtensions =

    [<Extension>]
    static member AddEventStore(services: IServiceCollection, config: IConfiguration, ?subscriber) =
        let subscriber = defaultArg subscriber false

        services
            .AddOptions<EventStoreOptions>()
            .Bind(config.GetSection(EventStoreOptions.Name))
            .ValidateDataAnnotations()
            .ValidateOnStart()
        |> ignore

        services.AddSingleton<ISettings, Settings>() |> ignore
        services.AddSingleton<IClient, Client>() |> ignore
        services.AddSingleton<IStream, Stream>() |> ignore

        if subscriber then
            services.AddSingleton<ISubscriber, Subscriber>() |> ignore
