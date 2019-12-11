namespace UniStream.Domain

open System


/// <summary>聚合仓储
/// <para>1、管理聚合的缓存，采用聚合ID为键的Map。</para>
/// <para>1.1、采用聚合ID为键的Map。</para>
/// <para>1.2、取用的聚合必然会在缓存Map内。</para>
/// <para>1.3、通过状态控制聚合的取用，超时则阻塞。</para>
/// <para>2、管理EventStore的读写。</para>
/// </summary>
/// <typeparam name="'agg">聚合的类型。</typeparam>
type internal Repository<'agg>

[<RequireQualifiedAccess>]
module internal Repository =

    /// <summary>初始化一个空的聚合仓储
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="get">由EventStore重建聚合的函数。</param>
    /// <param name="esFunc">领域事件流存储函数。</param>
    /// <param name="timeout">超时的Ticks约束。</param>
    val empty<'agg> :
        (string -> Guid -> (byte[] * byte[]) array) ->
        (string -> Guid -> string -> byte[] -> byte[] -> unit) ->
        int64 -> Repository<'agg>

[<Sealed>]
type internal Repository<'agg> with

    /// <summary>取出一个聚合
    /// <para>取用聚合都是为了执行一个命令。</para>
    /// </summary>
    /// <param name="id">聚合ID。</param>
    /// <param name="channel">返回聚合的通道。</param>
    member Take : (Guid -> AsyncReplyChannel<Result<'agg, string>> -> Repository<'agg>)

    /// <summary>保存聚合
    /// <para>1、持久化到领域事件流。</para>
    /// <para>2、保存新聚合到聚合仓储。</para>
    /// </summary>
    /// <param name="agg'">要保存的聚合。</param>
    /// <param name="metaTrace">要持久化的领域追踪元数据。</param>
    /// <param name="delta">要持久化的边际影响。</param>
    member Save : ('agg -> MetaTrace.T -> byte[] -> Repository<'agg>)

    /// <summary>放回聚合
    /// <para>由于命令执行失败，放回原来的聚合。</para>
    /// </summary>
    /// <param name="agg">要放回的聚合。</param>
    member Put : ('agg -> Repository<'agg>)