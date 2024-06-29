namespace UniStream.Domain

open System
open Microsoft.Extensions.Options
open Confluent.Kafka


/// <summary>Kafka生产者接口
/// </summary>
[<Interface>]
type IProducer =

    /// <summary>Kafka生产者
    /// </summary>
    abstract member Client: IProducer<byte array, byte array>


/// <summary>Kafka聚合类型生产者
/// </summary>
[<Sealed>]
type TypProducer =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka生产者配置选项。</param>
    new: options: IOptionsMonitor<ProducerConfig> -> TypProducer

    interface IProducer

    interface IDisposable


/// <summary>Kafka聚合生产者
/// </summary>
[<Sealed>]
type AggProducer =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka生产者配置选项。</param>
    new: options: IOptionsMonitor<ProducerConfig> -> AggProducer

    interface IProducer

    interface IDisposable


/// <summary>Kafka命令生产者
/// </summary>
[<Sealed>]
type ComProducer =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka生产者配置选项。</param>
    new: options: IOptionsMonitor<ProducerConfig> -> ComProducer

    interface IProducer

    interface IDisposable
