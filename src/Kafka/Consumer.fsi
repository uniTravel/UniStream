namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


/// <summary>Kafka消费者接口
/// </summary>
[<Interface>]
type IConsumer<'k, 'v> =

    /// <summary>Kafka消费者
    /// </summary>
    abstract member Client: Confluent.Kafka.IConsumer<'k, 'v>


/// <summary>Kafka聚合消费者
/// </summary>
[<Sealed>]
type AggregateConsumer =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    new: options: IOptionsMonitor<ConsumerConfig> -> AggregateConsumer

    interface IConsumer<string, byte array>

    interface IDisposable


/// <summary>Kafka命令消费者
/// </summary>
[<Sealed>]
type CommandConsumer =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka消费者配置选项。</param>
    new: options: IOptionsMonitor<ConsumerConfig> -> CommandConsumer

    interface IConsumer<string, byte array>

    interface IDisposable
