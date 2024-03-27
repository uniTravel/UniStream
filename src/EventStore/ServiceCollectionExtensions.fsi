namespace UniStream.Domain

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection


/// <summary>EventStore服务注入扩展
/// </summary>
[<Sealed>]
[<Extension>]
type ServiceCollectionExtensions =

    /// <summary>注入EventStore相关配置
    /// </summary>
    /// <param name="config">配置。</param>
    /// <param name="?subscriber">是否包含持久化订阅客户端，缺省为false。</param>
    [<Extension>]
    static member AddEventStore: services: IServiceCollection * config: IConfiguration * ?subscriber: bool -> unit
