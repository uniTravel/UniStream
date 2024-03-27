namespace UniStream.Domain

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection


[<Sealed>]
[<Extension>]
type ServiceCollectionExtensions =

    [<Extension>]
    static member AddAggregate<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        let name = typeof<'agg>.Name

        services
            .AddOptions<AggregateOptions>(name)
            .Bind(config.GetSection("Aggregate:" + name))
            .ValidateDataAnnotations()
            .ValidateOnStart()
        |> ignore
