namespace UniStream.Domain

open System


/// <summary>聚合仓储
/// <para>1、管理聚合的缓存，采用聚合ID为键的Map。</para>
/// <para>1.1、采用聚合ID为键的Map。</para>
/// <para>1.2、取用的聚合必然会在缓存Map内。</para>
/// <para>1.3、通过状态控制聚合的取用，超时则阻塞。</para>
/// <para>2、管理EventStore的读写。</para>
/// </summary>
/// <typeparam name="agg">聚合的类型。</typeparam>
type internal Repository<'agg when 'agg :> IAggregate>

[<RequireQualifiedAccess>]
module internal Repository =

    /// <summary>初始化一个空的聚合仓储
    /// </summary>
    val empty<'agg when 'agg :> IAggregate> : Repository<'agg>

[<Sealed>]
type internal Repository<'agg when 'agg :> IAggregate> with

    /// <summary>取出一个聚合
    /// <para>取用聚合都是为了执行一个命令。</para>
    /// </summary>
    /// <param name="id">聚合ID。</param>
    /// <param name="get">由EventStore重建聚合的函数。</param>
    /// <param name="timeout">超时的Ticks约束。</param>
    /// <param name="channel">返回聚合的通道。</param>
    member Take : (Guid -> (Guid -> 'agg) -> int64 -> AsyncReplyChannel<Result<'agg, string>> -> Repository<'agg>)

    /// <summary>放回一个聚合
    /// <para>1、命令执行成功，放回新的聚合。</para>
    /// <para>2、命令执行失败，放回原来的聚合。</para>
    /// </summary>
    /// <param name="agg">要放回的聚合。</param>
    /// <param name="timeout">超时的Ticks约束。</param>
    member Put : ('agg -> int64 -> Repository<'agg>)