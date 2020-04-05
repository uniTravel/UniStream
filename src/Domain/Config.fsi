namespace UniStream.Domain

open System


/// <summary>从某个版本开始获取聚合事件的函数
/// </summary>
type Get = Guid -> int64 -> (Guid * string * byte[])[] * int64

/// <summary>领域事件流存储函数
/// </summary>
type EsFunc = Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>

/// <summary>仓储模式
/// </summary>
/// <typeparam name="Cache">缓存模式。</typeparam>
/// <typeparam name="Snapshot">快照模式。</typeparam>
type RepoMode =
    | Cache of int64
    | Snapshot of int64 * int64 * int64

/// <summary>聚合仓储访问类型
/// </summary>
type Repo<'agg> =
    | Take of Guid * AsyncReplyChannel<Result<'agg * int64, string>>
    | Put of Guid * 'agg * int64
    | Refresh
    | Scavenge

/// <summary>批处理请求类型
/// </summary>
type Bat<'agg> =
    | Add of Guid * Guid * ('agg -> byte[] -> (string * byte[] * byte[]) seq * 'agg) * AsyncReplyChannel<string voption>
    | Launch of DiagnoseLog.Logger * Get * EsFunc * MailboxProcessor<Repo<'agg>>
    | Clean of DiagnoseLog.Logger


/// <summary>配置模块
/// </summary>
[<RequireQualifiedAccess>]
module Config =

    /// <summary>不可变聚合配置
    /// </summary>
    [<Sealed>]
    type Immutable =

        /// <summary>构造函数
        /// </summary>
        /// <param name="esFunc">领域事件流存储函数。</param>
        /// <param name="ldFunc">领域日志流存储函数。</param>
        /// <param name="lgFunc">诊断日志流存储函数。</param>
        new :
            esFunc: (string -> Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>) *
            ldFunc: (string -> string -> byte[] -> byte[] -> unit) *
            lgFunc: (string -> byte[] -> unit)  -> Immutable

        /// <summary>领域事件流存储函数
        /// </summary>
        member EsFunc : (string -> Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>)

        /// <summary>领域日志流存储函数
        /// </summary>
        member LdFunc : (string -> string -> byte[] -> byte[] -> unit)

        /// <summary>诊断日志流存储函数
        /// </summary>
        member LgFunc : (string -> byte[] -> unit)


    /// <summary>可变聚合配置
    /// <para>1、刷新缓存的间隔应介于10~60秒之间。</para>
    /// <para>2、清扫快照的间隔应介于1~24小时之间。</para>
    /// </summary>
    [<Sealed>]
    type Mutable =

        /// <summary>构造函数
        /// <para>刷新缓存间隔以秒为单位，清扫快照间隔以小时为单位。</para>
        /// </summary>
        /// <param name="get">从某个版本开始获取聚合事件的函数。</param>
        /// <param name="esFunc">领域事件流存储函数。</param>
        /// <param name="ldFunc">领域日志流存储函数。</param>
        /// <param name="lgFunc">诊断日志流存储函数。</param>
        /// <param name="?cacheMode">是否缓存模式：true为缓存模式，false为快照模式，缺省为true。</param>
        /// <param name="?refresh">刷新聚合缓存的间隔秒数，缺省为15秒。</param>
        /// <param name="?scavenge">清扫聚合快照的间隔小时数，缺省为2小时。</param>
        /// <param name="?threshold">快照间隔，缺省为1000。</param>
        /// <param name="?batch">批处理间隔毫秒数，缺省为55毫秒。</param>
        /// <param name="?block">挂起超过设定的秒数，阻塞聚合请求，缺省为3秒。</param>
        new :
            get: (string -> Guid -> int64 -> (Guid * string * byte[])[] * int64) *
            esFunc: (string -> Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>) *
            ldFunc: (string -> string -> byte[] -> byte[] -> unit) *
            lgFunc: (string -> byte[] -> unit) *
            ?cacheMode: bool *
            ?refresh: int64 *
            ?scavenge: int64 *
            ?threshold: int64 *
            ?batch: int *
            ?block: int64 -> Mutable

        /// <summary>从某个版本开始获取聚合事件的函数
        /// </summary>
        member Get : (string -> Guid -> int64 -> (Guid * string * byte[])[] * int64)

        /// <summary>领域事件流存储函数
        /// </summary>
        member EsFunc : (string -> Guid -> int64 -> (string * byte[] * byte[]) seq -> Async<int64>)

        /// <summary>领域日志流存储函数
        /// </summary>
        member LdFunc : (string -> string -> byte[] -> byte[] -> unit)

        /// <summary>诊断日志流存储函数
        /// </summary>
        member LgFunc : (string -> byte[] -> unit)

        /// <summary>仓储模式
        /// </summary>
        member RepoMode : RepoMode

        /// <summary>批处理周期
        /// </summary>
        member Batch : float

        /// <summary>挂起超过设定的Ticks，阻塞聚合请求
        /// </summary>
        member BlockTicks : int64


    /// <summary>观察者聚合配置
    /// <para>1、刷新缓存的间隔应介于1800~7200秒之间。</para>
    /// <para>2、清扫快照的间隔应介于24~72小时之间。</para>
    /// </summary>
    [<Sealed>]
    type Observer =

        /// <summary>构造函数
        /// </summary>
        /// <param name="get">从某个版本开始获取聚合事件的函数。</param>
        /// <param name="ldFunc">领域日志流存储函数。</param>
        /// <param name="lgFunc">诊断日志流存储函数。</param>
        /// <param name="subBuilder">订阅函数构造器。</param>
        /// <param name="observableType">被观察聚合类型。</param>
        /// <param name="?cacheMode">是否缓存模式：true为缓存模式，false为快照模式，缺省为true。</param>
        /// <param name="?refresh">刷新聚合缓存的间隔秒数，缺省为1800秒。</param>
        /// <param name="?scavenge">清扫聚合快照的间隔小时数，缺省为24小时。</param>
        /// <param name="?threshold">快照间隔，缺省为1000。</param>
        /// <param name="?block">挂起超过设定的秒数，阻塞聚合请求，缺省为3秒。</param>
        new :
            get: (string -> Guid -> int64 -> (Guid * string * byte[])[] * int64) *
            ldFunc: (string -> string -> byte[] -> byte[] -> unit) *
            lgFunc: (string -> byte[] -> unit) *
            subBuilder: ((Guid -> string -> int64 -> byte[] -> byte[] -> Async<unit>) -> unit) *
            observableType: string *
            ?cacheMode: bool *
            ?refresh: int64 *
            ?scavenge: int64 *
            ?threshold: int64 *
            ?block: int64 -> Observer

        /// <summary>从某个版本开始获取聚合事件的函数
        /// </summary>
        member Get : (string -> Guid -> int64 -> (Guid * string * byte[])[] * int64)

        /// <summary>领域日志流存储函数
        /// </summary>
        member LdFunc : (string -> string -> byte[] -> byte[] -> unit)

        /// <summary>诊断日志流存储函数
        /// </summary>
        member LgFunc : (string -> byte[] -> unit)

        /// <summary>订阅函数构造器
        /// </summary>
        member SubBuilder : ((Guid -> string -> int64 -> byte[] -> byte[] -> Async<unit>) -> unit)

        /// <summary>被观察聚合类型
        /// </summary>
        member ObservableType : string

        /// <summary>仓储模式
        /// </summary>
        member RepoMode : RepoMode

        /// <summary>挂起超过设定的Ticks，阻塞聚合请求
        /// </summary>
        member BlockTicks : int64