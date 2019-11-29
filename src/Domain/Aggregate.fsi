namespace UniStream.Domain

open System


[<Sealed>]
/// <summary>聚合
/// </summary>
/// <typeparam name="'agg">聚合类型。</typeparam>
type Aggregate<'agg when 'agg :> IAggregate> =

    /// <summary>构造函数
    /// </summary>
    /// <param name="get">重建聚合的函数。</param>
    /// <param name="esFunc">领域事件流存储函数。</param>
    /// <param name="ldFunc">领域日志流存储函数。</param>
    /// <param name="lgFunc">诊断日志流存储函数。</param>
    /// <param name="blockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
    new :
        (Guid -> 'agg) *
        (Guid -> int -> Guid -> string -> byte[] -> byte[] -> unit) *
        (string -> Guid -> string -> byte[] -> byte[] -> unit) *
        (string -> byte[] -> unit) *
        int64 -> Aggregate<'agg>

    /// <summary>应用命令
    /// </summary>
    /// <typeparam name="'v">领域值类型。</typeparam>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'e">领域事件类型。</typeparam>
    /// <param name="meta">领域追踪元数据。</param>
    /// <param name="command">待执行的命令。</param>
    /// <returns>命令执行结果。</returns>
    member Apply<'v, 'e when 'v :> IValue and 'e :> IDomainEvent<'v, 'agg>> : MetaTrace.T -> IDomainCommand<'v, 'agg, 'e> -> Async<unit>