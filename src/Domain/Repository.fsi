namespace UniStream.Domain

open System


[<RequireQualifiedAccess>]
module internal Repository =

    /// <summary>聚合仓储
    /// <para>1、管理聚合的缓存，采用聚合ID为键的Map。</para>
    /// <para>1.1、采用聚合ID为键的Map。</para>
    /// <para>1.2、取用的聚合必然会在缓存Map内。</para>
    /// <para>1.3、通过状态控制聚合的取用，超时则阻塞。</para>
    /// <para>2、管理EventStore的读写。</para>
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    type T<'agg>

    /// <summary>初始化一个空的聚合仓储
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="get">聚合的由EventStore重建聚合的函数。</param>
    /// <param name="esFunc">聚合的领域事件流存储函数。</param>
    /// <param name="timeout">聚合的超时的Ticks约束。</param>
    val empty :
        (Guid -> (byte[] * byte[])[]) ->
        (Guid -> string -> byte[] -> byte[] -> unit) ->
        int64 -> T<'agg>

    /// <summary>取出一个聚合
    /// <para>取用聚合都是为了执行一个命令。</para>
    /// </summary>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="id">聚合ID。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val take : T<'agg> -> Guid -> AsyncReplyChannel<Result<'agg, string>> -> T<'agg>

    /// <summary>保存聚合
    /// <para>1、持久化到领域事件流。</para>
    /// <para>2、保存新聚合到聚合仓储。</para>
    /// </summary>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="agg'">要保存的聚合。</param>
    /// <param name="metaTrace">相关的领域追踪元数据。</param>
    /// <param name="delta">要持久化的边际影响。</param>
    val save : T<'agg> -> 'agg -> MetaTrace.T -> byte[] -> T<'agg>

    /// <summary>放回聚合
    /// <para>由于命令执行失败，放回原来的聚合。</para>
    /// </summary>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="metaTrace">相关的领域追踪元数据。</param>
    /// <param name="agg">要放回的聚合。</param>
    val put :T<'agg> -> 'agg -> MetaTrace.T -> T<'agg>