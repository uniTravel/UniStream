namespace UniStream.Domain

open System


[<RequireQualifiedAccess>]
module Api =

    /// <summary>开放Api
    /// <para>单个Cqrs应用只需创建一个Api实例，用于注入一些基础配置。</para>
    /// </summary>
    /// <param name="Get">重建聚合的函数。</param>
    /// <param name="EsFunc">领域事件流存储函数。</param>
    /// <param name="LdFunc">领域日志流存储函数。</param>
    /// <param name="LgFunc">诊断日志流存储函数。</param>
    /// <param name="BlockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
    type T =
        { Get: (string -> Guid -> (byte[] * byte[])[])
          EsFunc: (string -> Guid -> string -> byte[] -> byte[] -> unit)
          LdFunc: (string -> Guid -> string -> byte[] -> byte[] -> unit)
          LgFunc: (string -> byte[] -> unit)
          BlockSeconds: int64 }

    /// <summary>创建Api
    /// </summary>
    /// <param name="get">重建聚合的函数。</param>
    /// <param name="esFunc">领域事件流存储函数。</param>
    /// <param name="ldFunc">领域日志流存储函数。</param>
    /// <param name="lgFunc">诊断日志流存储函数。</param>
    /// <param name="blockSeconds">挂起超过设定的秒数，阻塞聚合请求。</param>
    val create :
        (string -> Guid -> (byte[] * byte[])[]) ->
        (string -> Guid -> string -> byte[] -> byte[] -> unit) ->
        (string -> Guid -> string -> byte[] -> byte[] -> unit) ->
        (string -> byte[] -> unit) ->
        int64 -> T

    /// <summary>应用命令
    /// <para>面向流程管理器传入的命令。</para>
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="t">开放Api。</param>
    /// <param name="metaTrace">领域追踪元数据。</param>
    /// <param name="command">待执行的命令。</param>
    val inline applyCommand : T -> MetaTrace.T -> ^c -> Async<unit>
        when ^c : (member Value: 'd)
        and ^c : (member Apply: ('agg -> 'agg))

    /// <summary>应用命令
    /// <para>面向网络传入、数组格式的原始命令。</para>
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <typeparam name="^d">边际影响类型。</typeparam>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <param name="t">开放Api。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="cBytes">数组格式的原始命令。</param>
    /// <param name="f">创建命令的函数。</param>
    val inline applyRaw : T -> Guid -> byte[] -> (^d -> ^c) -> Async<unit>
        when ^c : (member Value: 'a)
        and ^c : (member Apply: ('agg -> 'agg))