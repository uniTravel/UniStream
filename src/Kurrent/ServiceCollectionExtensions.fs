namespace UniStream.Domain

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection


/// <summary>Kurrent服务注入扩展
/// </summary>
[<Sealed>]
type ServiceCollectionExtensions =

    /// <summary>注入命令发送者相关配置
    /// </summary>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender(services: IServiceCollection, config: IConfiguration) =
        if String.IsNullOrWhiteSpace config["Kurrent:Hostname"] then
            failwith "Configuration item [Kurrent:Hostname] required"

        services
            .AddOptions<KurrentOptions>()
            .Bind(config.GetSection KurrentOptions.Name)
            .ValidateDataAnnotations()
            .ValidateOnStart()
        |> ignore

        services
            .AddSingleton<ISettings, Settings>()
            .AddSingleton<IClient, Client>()
            .AddSingleton<IPersistent, Persistent>()

    /// <summary>注入聚合命令发送者相关配置
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services.AddCommand<'agg>(config).AddSingleton<ISender<'agg>, Sender<'agg>>()


    /// <summary>注入Kurrent初始化相关配置
    /// </summary>
    /// <remarks>启用投影、创建持久化订阅。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddInitializer(services: IServiceCollection, config: IConfiguration) =
        services
            .AddOptions<KurrentOptions>()
            .Bind(config.GetSection KurrentOptions.Name)
            .ValidateDataAnnotations()
            .ValidateOnStart()
        |> ignore

        services.AddSingleton<ISettings, Settings>().AddSingleton<IManager, Manager>()

    /// <summary>注入命令处理者相关配置
    /// </summary>
    /// <remarks>单节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddHandler(services: IServiceCollection, config: IConfiguration) =
        services
            .AddOptions<KurrentOptions>()
            .Bind(config.GetSection KurrentOptions.Name)
            .ValidateDataAnnotations()
            .ValidateOnStart()
        |> ignore

        services.AddSingleton<ISettings, Settings>().AddSingleton<IClient, Client>()

    /// <summary>注入聚合命令处理者相关配置
    /// </summary>
    /// <remarks>单节点执行命令。</remarks>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddHandler<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services.AddAggregate<'agg>(config).AddSingleton<IStream<'agg>, Stream<'agg>>()

    /// <summary>注入命令订阅者相关配置
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber(services: IServiceCollection, config: IConfiguration) =
        services
            .AddOptions<KurrentOptions>()
            .Bind(config.GetSection KurrentOptions.Name)
            .ValidateDataAnnotations()
            .ValidateOnStart()
        |> ignore

        services
            .AddSingleton<ISettings, Settings>()
            .AddSingleton<IClient, Client>()
            .AddSingleton<IPersistent, Persistent>()

    /// <summary>注入聚合命令订阅者相关配置
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services
            .AddAggregate<'agg>(config)
            .AddSingleton<ISubscriber<'agg>, Subscriber<'agg>>()
            .AddSingleton<IStream<'agg>, Stream<'agg>>()
