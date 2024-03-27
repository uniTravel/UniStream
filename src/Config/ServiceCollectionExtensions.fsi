namespace UniStream.Domain

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection


/// <summary>聚合配置扩展
/// </summary>
[<Sealed>]
[<Extension>]
type ServiceCollectionExtensions =

    /// <summary>增加聚合配置
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    [<Extension>]
    static member AddAggregate<'agg> :
        services: IServiceCollection * config: IConfiguration -> unit when 'agg :> Aggregate
