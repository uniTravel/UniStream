namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


/// <summary>Kafka消费者接口
/// </summary>
[<Interface>]
type IConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>Kafka消费者
    /// </summary>
    abstract member Client: IConsumer<byte array, byte array>


/// <summary>Kafka聚合类型消费者
/// </summary>
[<Sealed>]
type TypConsumer<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    new: options: IOptionsMonitor<ConsumerConfig> -> TypConsumer<'agg>

    interface IConsumer<'agg>

    interface IDisposable


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
    new: options: IOptionsMonitor<ConsumerConfig> -> ComConsumer<'agg>

    interface IConsumer<'agg>

    interface IDisposable
