namespace UniStream.Domain

open Microsoft.Extensions.Options
open Confluent.Kafka


/// <summary>Stream类型
/// </summary>
[<Sealed>]
type Stream =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="producerConfig">Kafka生产者配置。</param>
    /// <param name="producerConfig">Kafka消费者配置。</param>
    new: producerConfig: IOptionsMonitor<ProducerConfig> * consumerConfig: IOptionsMonitor<ConsumerConfig> -> Stream

    interface IStream
