namespace UniStream.Domain

open System
open System.Collections.Generic


/// <summary>聚合仓储模块
/// </summary>
[<RequireQualifiedAccess>]
module Repository =

    /// <summary>聚合状态
    /// </summary>
    /// <param name="Available">Available状态。</param>
    /// <param name="Empty">Empty状态。</param>
    /// <param name="Pending">Pending状态。</param>
    /// <param name="Blocked">Blocked状态，等待堆积的事件跨度超过设定阈值后阻塞。</param>
    type State<'agg> =
        | Available of 'agg * int64 * int64
        | Empty
        | Pending of int64
        | Blocked of int64

    /// <summary>聚合仓储
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="Get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="Timeout">聚合的超时Ticks约束。</param>
    /// <param name="Cache">聚合缓存，以聚合ID为键，值包括一个等待队列和聚合状态。</param>
    type T<'agg> =
        { Get: string -> int64 -> (Guid * string * byte[])[] * int64
          Timeout: int64
          Cache: Map<string, Queue<int64 * AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref> }

    /// <summary>聚合仓储快照
    /// <para>1、聚合ID作为键。</para>
    /// <para>2、对应的值为：聚合*版本*台阶*时间戳。</para>
    /// </summary>
    val snapshot<'agg> : Map<string, 'agg * int64 * int64 * int64 ref> ref

    /// <summary>初始化一个空的聚合仓储
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="get">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="timeout">聚合的超时Ticks约束。</param>
    val empty : DiagnoseLog.Logger -> (string -> int64 -> (Guid * string * byte[])[] * int64) -> int64 -> T<'agg>

    /// <summary>同步聚合
    /// <para>同步到流存储的最新状态。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="agg">当前聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="get">从某个版本开始获取聚合事件的函数。</param>
    /// <returns>当前的聚合及相应的聚合版本。</returns>
    val inline sync< ^agg> : DiagnoseLog.Logger -> string -> ^agg -> int64 -> (string -> int64 -> (Guid * string * byte[])[] * int64) -> (^agg * int64)
        when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>本地缓存获取聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val fromCache : DiagnoseLog.Logger -> T<'agg> -> string -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T<'agg>

    /// <summary>流存储获取聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="agg">起始聚合。</param>
    /// <param name="version">起始聚合版本。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline fromStore : DiagnoseLog.Logger -> T< ^agg> -> string -> ^agg -> int64 -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>
        when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>刷新聚合缓存
    /// <para>1、移除长时间未使用的聚合。</para>
    /// <para>2、移除长时间处于Blocked状态的聚合。</para>
    /// </summary>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="interval">以Ticks表示的间隔阈值。</param>
    val refresh : DiagnoseLog.Logger -> T<'agg> -> int64 -> T<'agg>

    /// <summary>清扫聚合快照
    /// <para>移除长时间未使用的聚合。</para>
    /// </summary>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="interval">以Ticks表示的间隔阈值。</param>
    val scavenge : DiagnoseLog.Logger -> int64 -> unit

    /// <summary>取出一个聚合
    /// <para>一般模式。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline gTake : DiagnoseLog.Logger -> T< ^agg> -> string -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>放回聚合
    /// <para>一般模式。</para>
    /// </summary>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="agg">要放回的聚合。</param>
    /// <param name="version">聚合版本。</param>
    val gPut : DiagnoseLog.Logger -> T<'agg> -> string -> 'agg -> int64 -> T<'agg>

    /// <summary>取出一个聚合
    /// <para>快照模式。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline sTake : DiagnoseLog.Logger -> T< ^agg> -> string -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>放回聚合
    /// <para>快照模式。</para>
    /// </summary>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="threshold">快照阈值。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="agg">要放回的聚合。</param>
    /// <param name="version">聚合版本。</param>
    val sPut : DiagnoseLog.Logger -> int64 -> T<'agg> -> string -> 'agg -> int64 -> T<'agg>