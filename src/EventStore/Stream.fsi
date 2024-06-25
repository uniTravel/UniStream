namespace UniStream.Domain

open Microsoft.Extensions.Logging


/// <summary>Stream类型
/// </summary>
[<Sealed>]
type Stream<'agg when 'agg :> Aggregate> =

    /// <summary>主构造函数
    /// </summary>
    /// <param name="logger">日志记录器。</param>
    /// <param name="client">EventStore客户端。</param>
    new: logger: ILogger<Stream<'agg>> * client: IClient -> Stream<'agg>

    interface IStream<'agg>
