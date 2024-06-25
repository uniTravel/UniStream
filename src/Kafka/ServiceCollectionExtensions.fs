namespace UniStream.Domain

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Confluent.Kafka
open UniStream.Domain


[<RequireQualifiedAccess>]
module internal Cfg =
    let admin (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<AdminClientConfig>().Bind(config.GetSection("Kafka:Admin"))
        |> ignore

        services.AddSingleton<IAdmin, Admin>() |> ignore

    let aggregateProducer (services: IServiceCollection) (config: IConfiguration) =
        services
            .AddOptions<ProducerConfig>(Cons.Agg)
            .Bind(config.GetSection("Kafka:Aggregate:Producer"))
        |> ignore

        services.AddKeyedSingleton<IProducer, AggregateProducer>(Cons.Agg) |> ignore

    let aggregateConsumer (services: IServiceCollection) (config: IConfiguration) =
        services
            .AddOptions<ConsumerConfig>(Cons.Agg)
            .Bind(config.GetSection("Kafka:Aggregate:Consumer"))
        |> ignore

        services.AddKeyedSingleton<IConsumer, AggregateConsumer>(Cons.Agg) |> ignore

    let commandProducer (services: IServiceCollection) (config: IConfiguration) =
        services
            .AddOptions<ProducerConfig>(Cons.Com)
            .Bind(config.GetSection("Kafka:Command:Producer"))
        |> ignore

        services.AddKeyedSingleton<IProducer, CommandProducer>(Cons.Com) |> ignore

    let commandConsumer (services: IServiceCollection) (config: IConfiguration) =
        services
            .AddOptions<ConsumerConfig>(Cons.Com)
            .Bind(config.GetSection("Kafka:Command:Consumer"))
        |> ignore

        services.AddKeyedSingleton<IConsumer, CommandConsumer>(Cons.Com) |> ignore


/// <summary>Kafka服务注入扩展
/// </summary>
[<Sealed>]
type ServiceCollectionExtensions =

    /// <summary>注入命令发送者相关配置
    /// </summary>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender(services: IServiceCollection, config: IConfiguration) =
        Cfg.commandProducer services config
        Cfg.commandConsumer services config

    /// <summary>注入Kafka初始化相关配置
    /// </summary>
    /// <remarks>启用投影、创建持久化订阅。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddInitializer(services: IServiceCollection, config: IConfiguration) = Cfg.admin services config

    /// <summary>注入命令处理者相关配置
    /// </summary>
    /// <remarks>单节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddHandler(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        Cfg.aggregateProducer services config
        Cfg.aggregateConsumer services config

    /// <summary>注入聚合投影者相关配置
    /// </summary>
    /// <remarks>监听并投影到聚合流。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddProjector(services: IServiceCollection, config: IConfiguration) =
        Cfg.aggregateProducer services config
        Cfg.aggregateConsumer services config

    /// <summary>注入命令订阅者相关配置
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        Cfg.aggregateProducer services config
        Cfg.aggregateConsumer services config
        Cfg.commandProducer services config
        Cfg.commandConsumer services config
