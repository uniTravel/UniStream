namespace UniStream.Domain

open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Confluent.Kafka
open UniStream.Domain


[<RequireQualifiedAccess>]
module internal Cfg =
    let admin (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<AdminClientConfig>().Bind(config.GetSection "Kafka:Admin")
        |> ignore

        services.AddSingleton<IAdmin, Admin>() |> ignore

    let typProducer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ProducerConfig>(Cons.Typ).Bind(config.GetSection "Kafka:Typ:Producer")
        |> ignore

        services.AddKeyedSingleton<IProducer, TypProducer> Cons.Typ |> ignore

    let typConsumer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ConsumerConfig>(Cons.Typ).Bind(config.GetSection "Kafka:Typ:Consumer")
        |> ignore

        services.AddKeyedSingleton<IConsumer, TypConsumer> Cons.Typ |> ignore

    let aggProducer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ProducerConfig>(Cons.Agg).Bind(config.GetSection "Kafka:Agg:Producer")
        |> ignore

        services.AddKeyedSingleton<IProducer, AggProducer> Cons.Agg |> ignore

    let aggConsumer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ConsumerConfig>(Cons.Agg).Bind(config.GetSection "Kafka:Agg:Consumer")
        |> ignore

    let comProducer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ProducerConfig>(Cons.Com).Bind(config.GetSection "Kafka:Com:Producer")
        |> ignore

        services.AddKeyedSingleton<IProducer, ComProducer> Cons.Com |> ignore

    let comConsumer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ConsumerConfig>(Cons.Com).Bind(config.GetSection "Kafka:Com:Consumer")
        |> ignore

        services.AddKeyedSingleton<IConsumer, ComConsumer> Cons.Com |> ignore


/// <summary>Kafka服务注入扩展
/// </summary>
[<Sealed>]
type ServiceCollectionExtensions =

    /// <summary>注入命令发送者相关配置
    /// </summary>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        Cfg.comProducer services config
        Cfg.typConsumer services config
        services

    /// <summary>注入聚合命令发送者相关配置
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services.AddCommand<'agg>(config).AddSingleton<ISender<'agg>, Sender<'agg>>()

    /// <summary>注入Kafka初始化相关配置
    /// </summary>
    /// <remarks>启用投影、创建持久化订阅。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddInitializer(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        services

    /// <summary>注入命令处理者相关配置
    /// </summary>
    /// <remarks>单节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddHandler(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        Cfg.typProducer services config
        Cfg.typConsumer services config
        Cfg.aggConsumer services config
        services

    /// <summary>注入聚合命令处理者相关配置
    /// </summary>
    /// <remarks>单节点执行命令。</remarks>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddHandler<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services.AddAggregate<'agg>(config).AddSingleton<IStream<'agg>, Stream<'agg>>()

    /// <summary>注入聚合投影者相关配置
    /// </summary>
    /// <remarks>监听并投影到聚合流。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddProjector(services: IServiceCollection, config: IConfiguration) =
        Cfg.typConsumer services config
        Cfg.aggProducer services config
        services

    /// <summary>注入命令订阅者相关配置
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        Cfg.comConsumer services config
        Cfg.typProducer services config
        Cfg.typConsumer services config
        Cfg.aggConsumer services config
        services

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
