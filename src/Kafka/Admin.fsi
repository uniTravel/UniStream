namespace UniStream.Domain

open Microsoft.Extensions.Options
open Confluent.Kafka


/// <summary>Kafka管理客户端接口
/// </summary>
[<Interface>]
type IAdmin =

    /// <summary>Kafka管理客户端
    /// </summary>
    abstract member Client: IAdminClient


/// <summary>Kafka管理客户端
/// </summary>
[<Sealed>]
type Admin =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="options">Kafka管理客户端配置选项。</param>
    new: options: IOptions<AdminClientConfig> -> Admin

    interface IAdmin
