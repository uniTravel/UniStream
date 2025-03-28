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

    let typProducer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ProducerConfig>(Cons.Typ).Bind(config.GetSection "Kafka:Typ:Producer")
        |> ignore

    let typConsumer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ConsumerConfig>(Cons.Typ).Bind(config.GetSection "Kafka:Typ:Consumer")
        |> ignore

    let aggProducer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ProducerConfig>(Cons.Agg).Bind(config.GetSection "Kafka:Agg:Producer")
        |> ignore

    let aggConsumer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ConsumerConfig>(Cons.Agg).Bind(config.GetSection "Kafka:Agg:Consumer")
        |> ignore

    let comProducer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ProducerConfig>(Cons.Com).Bind(config.GetSection "Kafka:Com:Producer")
        |> ignore

    let comConsumer (services: IServiceCollection) (config: IConfiguration) =
        services.AddOptions<ConsumerConfig>(Cons.Com).Bind(config.GetSection "Kafka:Com:Consumer")
        |> ignore


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

    /// <summary>注入聚合命令发送者
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services
            .AddCommand<'agg>(config)
            .AddSingleton<IAdmin<'agg>, Admin<'agg>>()
            .AddSingleton<IProducer<'agg>, ComProducer<'agg>>()
            .AddSingleton<IConsumer<'agg>, TypConsumer<'agg>>()
            .AddSingleton<ISender<'agg>, Sender<'agg>>()

    /// <summary>注入Kafka初始化相关配置
    /// </summary>
    /// <remarks>启用投影、创建持久化订阅。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddInitializer(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        services

    /// <summary>注入投影者相关配置
    /// </summary>
    /// <remarks>监听并投影到聚合流。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddProjector(services: IServiceCollection, config: IConfiguration) =
        Cfg.typConsumer services config
        Cfg.aggProducer services config
        services

    /// <summary>注入聚合投影者
    /// </summary>
    /// <remarks>监听并投影到聚合流。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddProjector<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services
            .AddSingleton<IConsumer<'agg>, TypConsumer<'agg>>()
            .AddSingleton<IProducer<'agg>, AggProducer<'agg>>()
            .AddSingleton<IWorker<'agg>, Projector<'agg>>()

    /// <summary>注入命令订阅者相关配置
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config
        Cfg.typConsumer services config
        Cfg.comConsumer services config
        Cfg.typProducer services config
        Cfg.aggConsumer services config
        services

    /// <summary>注入聚合命令订阅者
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        services
            .AddAggregate<'agg>(config)
            .AddSingleton<IAdmin<'agg>, Admin<'agg>>()
            .AddKeyedSingleton<IConsumer<'agg>, TypConsumer<'agg>>(Cons.Typ)
            .AddKeyedSingleton<IConsumer<'agg>, ComConsumer<'agg>>(Cons.Com)
            .AddSingleton<IProducer<'agg>, TypProducer<'agg>>()
            .AddKeyedSingleton<IConsumer<'agg>, AggConsumer<'agg>>(Cons.Agg)
            .AddSingleton<ISubscriber<'agg>, Subscriber<'agg>>()
            .AddSingleton<IStream<'agg>, Stream<'agg>>()
