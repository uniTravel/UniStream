namespace UniStream.Domain

open System


/// <summary>观察者聚合器模块
/// <para>通过聚合键连接事件流。</para>
/// </summary>
[<RequireQualifiedAccess>]
module Observer =

    /// <summary>观察者聚合器消息类型
    /// </summary>
    /// <typeparam name="Append">附加领域事件：聚合键*领域事件版本*领域事件类型*领域事件数据。</typeparam>
    /// <typeparam name="Refresh">刷新缓存。</typeparam>
    /// <typeparam name="Scavenge">清扫快照。</typeparam>
    /// <typeparam name="Get">取出当前聚合。</typeparam>
    type Msg<'agg> =
        | Append of string * uint64 * string * ReadOnlyMemory<byte>
        | Refresh
        | Scavenge
        | Get of string * AsyncReplyChannel<Result<'agg, string>>

    /// <summary>观察者聚合器
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="Agent">聚合器代理。</typeparam>
    type T<'agg> =
        { Agent: MailboxProcessor<Msg<'agg>> }

    /// <summary>创建观察者聚合器
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="cfg">聚合器配置。</param>
    val inline create :
        cfg: Config.Observer ->
        T< ^agg >
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))

    /// <summary>附加领域事件
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    /// <param name="number">领域事件版本。</param>
    /// <param name="evType">领域事件类型。</param>
    /// <param name="data">领域事件数据。</param>
    val inline append :
        aggregator: T< ^agg> ->
        aggKey: string ->
        number: uint64 ->
        evType: string ->
        data: ReadOnlyMemory<byte> ->
        Async<unit>

    /// <summary>取出当前聚合
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <typeparam name="^v">聚合值类型。</typeparam>
    /// <param name="aggregator">聚合器。</param>
    /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
    val inline get :
        aggregator: T< ^agg> ->
        aggKey: string ->
        Async< ^v>
        when ^agg : (member Value : ^v)