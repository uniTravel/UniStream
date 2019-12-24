namespace UniStream.Domain

open System
open System.Collections.Generic


[<RequireQualifiedAccess>]
module Repository =

    /// <summary>聚合状态
    /// </summary>
    /// <param name="Available">Available状态。</param>
    /// <param name="Empty">Empty状态。</param>
    /// <param name="Pending">Pending状态。</param>
    /// <param name="Blocked">Blocked状态，等待堆积的事件跨度超过设定阈值后阻塞。</param>
    type State<'agg> =
        | Available of 'agg * int64
        | Empty
        | Pending of int64
        | Blocked

    /// <summary>聚合仓储
    /// <para>1、管理聚合的缓存，采用聚合ID为键的Map。</para>
    /// <para>1.1、采用聚合ID为键的Map。</para>
    /// <para>1.2、取用的聚合必然会在缓存Map内。</para>
    /// <para>1.3、通过状态控制聚合的取用，超时则阻塞。</para>
    /// <para>2、管理EventStore的读写。</para>
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="Get">获取聚合全部事件的函数。</param>
    /// <param name="Timeout">聚合的超时Ticks约束。</param>
    /// <param name="Map">聚合Map，以聚合ID为键，值包括一个等待队列和聚合状态。</param>
    type T<'agg> =
        { Get: Guid -> (Guid * string * byte[])[] * int64
          Timeout: int64
          Map: Map<Guid, Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref> }

    /// <summary>初始化一个空的聚合仓储
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="get">获取聚合全部事件的函数。</param>
    /// <param name="timeout">聚合的超时Ticks约束。</param>
    val empty : (Guid -> (Guid * string * byte[])[] * int64) -> int64 -> T<'agg>

    /// <summary>刷新聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="agg">当前聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="getFrom">从某个版本开始获取聚合事件的函数。</param>
    /// <returns>刷新后的聚合及相应的聚合版本。</returns>
    val inline refresh< ^agg> : Guid -> ^agg -> int64 -> (Guid -> int64 -> (Guid * string * byte[])[] * int64) -> (^agg * int64)
        when ^agg : (member Apply : (string -> byte[] -> ^agg))

    /// <summary>取出一个聚合
    /// <para>取用聚合都是为了执行一个命令。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline take : T< ^agg> -> Guid -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>
        when ^agg : (static member Empty : ^agg)
        and ^agg : (member Apply : (string -> byte[] -> ^agg))

    /// <summary>放回聚合
    /// <para>由于命令执行失败，放回原来的聚合。</para>
    /// </summary>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="agg">要放回的聚合。</param>
    /// <param name="version">聚合版本。</param>
    val put :T<'agg> -> Guid -> 'agg -> int64 -> T<'agg>