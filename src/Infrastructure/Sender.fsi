namespace UniStream.Domain

open System
open System.Timers


/// <summary>聚合命令发送者模块
/// </summary>
[<RequireQualifiedAccess>]
module Sender =

    /// <summary>创建定时任务
    /// </summary>
    /// <param name="interval">触发任务间隔，单位毫秒。</param>
    /// <param name="work">待执行的任务。</param>
    val timer: interval: float -> work: (ElapsedEventArgs -> unit) -> Async<unit>

    /// <summary>发送命令
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <typeparam name="'com">命令类型。</typeparam>
    /// <typeparam name="'evt">事件类型。</typeparam>
    /// <param name="sender">聚合命令发送者。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="comId">命令ID。</param>
    /// <param name="com">命令。</param>
    val inline send<'agg, 'com, 'evt> :
        sender: ISender<'agg> -> aggId: Guid -> comId: Guid -> com: 'com -> Async<unit> when Com<'agg, 'com, 'evt>
