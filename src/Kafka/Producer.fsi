namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


/// <summary>Kafka生产者接口
/// </summary>
[<Interface>]
type IProducer<'k, 'v> =

    /// <summary>Kafka生产者
    /// </summary>
    abstract member Client: Confluent.Kafka.IProducer<'k, 'v>


/// <summary>Kafka聚合生产者
/// </summary>
[<Sealed>]
type AggregateProducer =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka生产者配置选项。</param>
    new: options: IOptionsMonitor<ProducerConfig> -> AggregateProducer

    interface IProducer<string, byte array>

    interface IDisposable


/// <summary>Kafka命令生产者
/// </summary>
[<Sealed>]
type CommandProducer =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka生产者配置选项。</param>
    new: options: IOptionsMonitor<ProducerConfig> -> CommandProducer

    interface IProducer<string, byte array>

    interface IDisposable
