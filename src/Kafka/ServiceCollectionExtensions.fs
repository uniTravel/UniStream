namespace UniStream.Domain

open System
open System.Runtime.CompilerServices
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Confluent.Kafka
open UniStream.Domain


/// <summary>EventStore服务注入扩展
/// </summary>
[<Sealed>]
type ServiceCollectionExtensions =

    /// <summary>注入命令发送者相关配置
    /// </summary>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSender(services: IServiceCollection, config: IConfiguration) =
        services
            .AddOptions<ProducerConfig>()
            .Bind(config.GetSection("Kafka:ProducerSettings"))
        |> ignore

        services
            .AddOptions<ConsumerConfig>()
            .Bind(config.GetSection("Kafka:ConsumerSettings"))
        |> ignore

    // services.AddSingleton<IProducer<Guid, EventData>, Producer<Guid, EventData>>()
    // |> ignore

    // services.AddSingleton<IConsumer<Guid, EventData>, Consumer<Guid, EventData>>()
    // |> ignore


    /// <summary>注入Kafka初始化相关配置
    /// </summary>
    /// <remarks>启用投影、创建持久化订阅。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddInitializer(services: IServiceCollection, config: IConfiguration) =
        services.AddOptions<AdminClientConfig>().Bind(config.GetSection("Kafka:Admin"))
        |> ignore

        services.AddSingleton<IAdmin, Admin>() |> ignore


    /// <summary>注入命令处理者相关配置
    /// </summary>
    /// <remarks>单节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddHandler(services: IServiceCollection, config: IConfiguration) =
        services
            .AddOptions<ProducerConfig>("Aggregate")
            .Bind(config.GetSection("Kafka:Aggregate:Producer"))
        |> ignore

        services
            .AddOptions<ConsumerConfig>("Aggregate")
            .Bind(config.GetSection("Kafka:Aggregate:Consumer"))
        |> ignore

        services.AddSingleton<IStream, Stream>() |> ignore


    /// <summary>注入命令订阅者相关配置
    /// </summary>
    /// <remarks>多节点执行命令。</remarks>
    /// <param name="config">配置。</param>
    [<Extension>]
    static member AddSubscriber(services: IServiceCollection, config: IConfiguration) =
        services
            .AddOptions<ProducerConfig>("Aggregate")
            .Bind(config.GetSection("Kafka:Aggregate:Producer"))
        |> ignore

        services
            .AddOptions<ConsumerConfig>("Aggregate")
            .Bind(config.GetSection("Kafka:Aggregate:Consumer"))
        |> ignore

        services
            .AddOptions<ProducerConfig>("Command")
            .Bind(config.GetSection("Kafka:Command:Producer"))
        |> ignore

        services
            .AddOptions<ConsumerConfig>("Command")
            .Bind(config.GetSection("Kafka:Command:Consumer"))
        |> ignore

        services.AddSingleton<IStream, Stream>() |> ignore
