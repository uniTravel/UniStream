namespace UniStream.Domain

open System
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

    let inline typProducer<'agg when 'agg :> Aggregate> (services: IServiceCollection) (config: IConfiguration) =
        let aggType = typeof<'agg>.Name

        services.AddOptions<ProducerConfig>(Cons.Typ + aggType).Bind(config.GetSection $"Kafka:TypProducer:{aggType}")
        |> ignore

    let inline aggProducer<'agg when 'agg :> Aggregate> (services: IServiceCollection) (config: IConfiguration) =
        let aggType = typeof<'agg>.Name

        services.AddOptions<ProducerConfig>(Cons.Agg + aggType).Bind(config.GetSection $"Kafka:AggProducer:{aggType}")
        |> ignore

    let inline comProducer<'agg when 'agg :> Aggregate> (services: IServiceCollection) (config: IConfiguration) =
        let aggType = typeof<'agg>.Name

        services.AddOptions<ProducerConfig>(Cons.Com + aggType).Bind(config.GetSection $"Kafka:ComProducer:{aggType}")
        |> ignore

    let inline typConsumer<'agg when 'agg :> Aggregate> (services: IServiceCollection) (config: IConfiguration) =
        let aggType = typeof<'agg>.Name

        services.AddOptions<ConsumerConfig>(Cons.Typ + aggType).Bind(config.GetSection $"Kafka:TypConsumer:{aggType}")
        |> ignore

    let inline aggConsumer<'agg when 'agg :> Aggregate> (services: IServiceCollection) (config: IConfiguration) =
        let aggType = typeof<'agg>.Name

        services.AddOptions<ConsumerConfig>(Cons.Agg + aggType).Bind(config.GetSection $"Kafka:AggConsumer:{aggType}")
        |> ignore

    let inline comConsumer<'agg when 'agg :> Aggregate> (services: IServiceCollection) (config: IConfiguration) =
        let aggType = typeof<'agg>.Name

        services.AddOptions<ConsumerConfig>(Cons.Com + aggType).Bind(config.GetSection $"Kafka:ComConsumer:{aggType}")
        |> ignore


/// <summary>Kafka服务注入扩展
/// </summary>
[<Sealed>]
type ServiceCollectionExtensions =

    /// <summary>注入Kafka初始化相关配置
    /// </summary>
    /// <remarks>启用投影、创建持久化订阅。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddInitializer(services: IServiceCollection, config: IConfiguration) =
        Cfg.admin services config

        services.PostConfigureAll<AdminClientConfig>(fun options ->
            options.BootstrapServers <- config["Kafka:Bootstrap"])

    /// <summary>注入命令发送者相关配置
    /// </summary>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender(services: IServiceCollection, config: IConfiguration) =
        if String.IsNullOrWhiteSpace config["Kafka:Hostname"] then
            failwith "Configuration item [Kafka:Hostname] required"

        Cfg.admin services config

        services
            .PostConfigureAll<AdminClientConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])
            .PostConfigureAll<ProducerConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])
            .PostConfigureAll<ConsumerConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])

    /// <summary>注入聚合命令发送者
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member inline AddSender<'agg when 'agg :> Aggregate>(services: IServiceCollection, config: IConfiguration) =
        Cfg.comProducer<'agg> services config
        Cfg.typConsumer<'agg> services config

        services
            .AddCommand<'agg>(config)
            .AddSingleton<IAdmin<'agg>, Admin<'agg>>()
            .AddSingleton<IProducer<'agg>, ComProducer<'agg>>()
            .AddSingleton<IConsumer<'agg>, ReceiveConsumer<'agg>>()
            .AddSingleton<ISender<'agg>, Sender<'agg>>()

    /// <summary>注入投影者相关配置
    /// </summary>
    /// <remarks>监听并投影到聚合流。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddProjector(services: IServiceCollection, config: IConfiguration) =
        if String.IsNullOrWhiteSpace config["Kafka:Hostname"] then
            failwith "Configuration item [Kafka:Hostname] required"

        services
            .PostConfigureAll<ProducerConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])
            .PostConfigureAll<ConsumerConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])

    /// <summary>注入聚合投影者
    /// </summary>
    /// <remarks>监听并投影到聚合流。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member inline AddProjector<'agg when 'agg :> Aggregate>
        (services: IServiceCollection, config: IConfiguration)
        =
        Cfg.aggProducer<'agg> services config
        Cfg.typConsumer<'agg> services config

        services
            .AddSingleton<IProducer<'agg>, AggProducer<'agg>>()
            .AddSingleton<IConsumer<'agg>, ProjectConsumer<'agg>>()
            .AddHostedService<Projector<'agg>>()

    /// <summary>注入命令订阅者相关配置
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber(services: IServiceCollection, config: IConfiguration) =
        if String.IsNullOrWhiteSpace config["Kafka:Hostname"] then
            failwith "Configuration item [Kafka:Hostname] required"

        Cfg.admin services config

        services
            .PostConfigureAll<AdminClientConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])
            .PostConfigureAll<ProducerConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])
            .PostConfigureAll<ConsumerConfig>(fun options -> options.BootstrapServers <- config["Kafka:Bootstrap"])

    /// <summary>注入聚合命令订阅者
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member inline AddSubscriber<'agg when 'agg :> Aggregate>
        (services: IServiceCollection, config: IConfiguration)
        =
        Cfg.typProducer<'agg> services config
        Cfg.typConsumer<'agg> services config
        Cfg.comConsumer<'agg> services config
        Cfg.aggConsumer<'agg> services config

        services
            .AddAggregate<'agg>(config)
            .AddSingleton<IAdmin<'agg>, Admin<'agg>>()
            .AddSingleton<IProducer<'agg>, TypProducer<'agg>>()
            .AddKeyedSingleton<IConsumer<'agg>, RestoreConsumer<'agg>>(Cons.Typ)
            .AddKeyedSingleton<IConsumer<'agg>, ComConsumer<'agg>>(Cons.Com)
            .AddKeyedSingleton<IConsumer<'agg>, AggConsumer<'agg>>(Cons.Agg)
            .AddSingleton<ISubscriber<'agg>, Subscriber<'agg>>()
            .AddSingleton<IStream<'agg>, Stream<'agg>>()
