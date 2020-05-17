namespace UniStream.Domain

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
    type State<'agg> =
        | Available of 'agg * int64
        | Empty
        | Pending

    /// <summary>聚合仓储
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="Reader">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="Logger">诊断日志记录器。</param>
    /// <param name="Capacity">缓存与快照的容量。</param>
    /// <param name="Cache">聚合缓存，以聚合ID为键，值包括一个等待队列和聚合状态。</param>
    /// <param name="CacheUsage">聚合缓存使用记录。</param>
    /// <param name="Snapshot">聚合仓储快照，以聚合ID为键，对应的值为：聚合*版本*台阶*时间戳。</param>
    /// <param name="SnapUsage">聚合仓储快照使用记录。</param>
    type T<'agg> =
        { Reader: Reader
          Logger: DiagnoseLog.Logger
          Capacity: int
          Cache: Dictionary<string, Queue<AsyncReplyChannel<Result<'agg * int64, string>>> * State<'agg> ref>
          CacheUsage: string list
          Snapshot: Dictionary<string, 'agg * int64 * int64>
          SnapUsage: string list }

    /// <summary>同步聚合
    /// <para>同步到流存储的最新状态。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    /// <param name="agg">当前聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="reader">从某个版本开始获取聚合事件的函数。</param>
    /// <returns>当前的聚合及相应的聚合版本。</returns>
    val inline sync< ^agg> : DiagnoseLog.Logger -> string -> ^agg -> int64 -> Reader -> (^agg * int64)
        when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>本地缓存获取聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline fromCache : T< ^agg> -> string -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>

    /// <summary>流存储获取聚合
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    /// <param name="agg">起始聚合。</param>
    /// <param name="version">起始聚合版本。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline fromStore : T< ^agg>-> string -> ^agg -> int64 -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>
        when ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>刷新聚合缓存
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    val inline refresh : T< ^agg> -> T< ^agg> * string seq

    /// <summary>清扫聚合快照
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    val inline scavenge : T< ^agg> -> T< ^agg>

    /// <summary>占用一个聚合
    /// <para>经由缓存处理。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline cTake : T< ^agg> -> string -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>放回聚合
    /// <para>经由缓存处理。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    /// <param name="agg">要放回的聚合。</param>
    /// <param name="version">聚合版本。</param>
    val inline cPut : T< ^agg> -> string -> ^agg -> int64 -> T< ^agg>

    /// <summary>占用一个聚合
    /// <para>经由缓存/快照处理。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    /// <param name="channel">返回聚合的通道。</param>
    val inline sTake : T< ^agg> -> string -> AsyncReplyChannel<Result< ^agg * int64, string>> -> T< ^agg>
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> byte[] -> ^agg))

    /// <summary>放回聚合
    /// <para>经由缓存/快照处理。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合的类型。</typeparam>
    /// <param name="threshold">快照阈值。</param>
    /// <param name="repo">聚合仓储。</param>
    /// <param name="id">聚合ID或关联Key。</param>
    /// <param name="agg">要放回的聚合。</param>
    /// <param name="version">聚合版本。</param>
    val inline sPut : int64 -> T< ^agg> -> string -> ^agg -> int64 -> T< ^agg>

    /// <summary>初始化一个空的聚合仓储
    /// </summary>
    /// <typeparam name="'agg">聚合的类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="reader">从某个版本开始获取聚合事件的函数。</param>
    /// <param name="capacity">缓存与快照的容量。</param>
    val empty : DiagnoseLog.Logger -> Reader -> int -> T<'agg>