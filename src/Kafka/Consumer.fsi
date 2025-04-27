namespace UniStream.Domain

open System
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Options
open Confluent.Kafka


/// <summary>Kafka消费者接口
/// </summary>
[<Interface>]
type IConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>Kafka消费者
    /// </summary>
    abstract member Client: IConsumer<byte array, byte array>


/// <summary>Kafka聚合消费者
/// </summary>
[<Sealed>]
type AggConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    new: options: IOptionsMonitor<ConsumerConfig> -> AggConsumer<'agg>

    interface IConsumer<'agg>

    interface IDisposable


/// <summary>Kafka命令消费者
/// </summary>
[<Sealed>]
type ComConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    /// <param name="config">应用配置。</param>
    new: options: IOptionsMonitor<ConsumerConfig> * config: IConfiguration -> ComConsumer<'agg>

    interface IConsumer<'agg>

    interface IDisposable


/// <summary>Kafka聚合类型消费者
/// </summary>
/// <remarks>接收命令执行结果。</remarks>
[<Sealed>]
type ReceiveConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    /// <param name="config">应用配置。</param>
    new: options: IOptionsMonitor<ConsumerConfig> * config: IConfiguration -> ReceiveConsumer<'agg>

    interface IConsumer<'agg>

    interface IDisposable


/// <summary>Kafka聚合类型消费者
/// </summary>
/// <remarks>初始化命令操作历史缓存。</remarks>
[<Sealed>]
type RestoreConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    new: options: IOptionsMonitor<ConsumerConfig> -> RestoreConsumer<'agg>

    interface IConsumer<'agg>

    interface IDisposable


/// <summary>Kafka聚合类型消费者
/// </summary>
/// <remarks>投影生成聚合流。</remarks>
[<Sealed>]
type ProjectConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    new: options: IOptionsMonitor<ConsumerConfig> -> ProjectConsumer<'agg>

    interface IConsumer<'agg>

    interface IDisposable
