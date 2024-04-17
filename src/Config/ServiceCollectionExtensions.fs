namespace UniStream.Domain

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection


/// <summary>聚合配置注入扩展
/// </summary>
[<Sealed>]
type ServiceCollectionExtensions =

    /// <summary>聚合注入配置
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    [<Extension>]
    static member AddAggregate<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        let name = typeof<'agg>.Name

        services
            .AddOptions<AggregateOptions>(name)
            .Bind(config.GetSection("Aggregate:" + name))
            .ValidateDataAnnotations()
            .ValidateOnStart()
        |> ignore
