namespace UniStream.Domain

open System
open System.Timers


/// <summary>聚合工厂模块
/// </summary>
[<RequireQualifiedAccess>]
module internal Factory =

    /// <summary>异步返回聚合
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="channel">返回聚合的管道。</param>
    /// <param name="result">聚合，或者异常消息。</param>
    val inline reply :
        channel: AsyncReplyChannel<Result< ^agg, string>> ->
        result: Result< ^agg, string> ->
        Async<unit>

    /// <summary>执行领域命令
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="writer">基于特定聚合，领域事件流存储函数。</param>
    /// <param name="agg">聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="cmd">领域命令。</param>
    /// <returns>聚合及存储的领域事件数量。</returns>
    val inline private build :
        lg: DiagnoseLog.T ->
        writer: (uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) ->
        agg: ^agg ->
        version: uint64 ->
        cmd:(string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>> * AsyncReplyChannel<Result<'agg, string>>) ->
        ^agg * int
        when ^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg))

    /// <summary>批量执行领域命令
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="writer">基于特定聚合，领域事件流存储函数。</param>
    /// <param name="agg">聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="cmds">领域命令列表。</param>
    /// <returns>聚合及存储的领域事件数量。</returns>
    val inline private batch :
        lg: DiagnoseLog.T ->
        writer: (uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) ->
        agg: ^agg ->
        version: uint64 ->
        cmds:(string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>> * AsyncReplyChannel<Result<'agg, string>>) list ->
        ^agg * int
        when ^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg))

    /// <summary>初始化聚合工厂
    /// <para>单纯从存储获取领域事件集合。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="reader">基于特定聚合，从某个版本开始获取领域事件的函数。</param>
    /// <param name="snapshot">或有的聚合快照。</param>
    /// <returns>聚合及相应版本。</returns>
    val inline raw :
        lg: DiagnoseLog.T ->
        reader: (uint64 -> (uint64 * string * ReadOnlyMemory<byte>) seq) ->
        snapshot: (^agg * uint64) voption ->
        ^agg * uint64
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))

    /// <summary>初始化聚合工厂
    /// <para>从存储获取领域事件集合，并应用一条领域命令。</para>
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="reader">基于特定聚合，从某个版本开始获取领域事件的函数。</param>
    /// <param name="writer">基于特定聚合，领域事件流存储函数。</param>
    /// <param name="snapshot">或有的聚合快照。</param>
    /// <param name="cmd">领域命令。</param>
    /// <returns>聚合及相应版本。</returns>
    val inline init :
        lg: DiagnoseLog.T ->
        reader: (uint64 -> (uint64 * string * ReadOnlyMemory<byte>) seq) ->
        writer: (uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) ->
        snapshot: (^agg * uint64) voption ->
        cmd:(string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>> * AsyncReplyChannel<Result<'agg, string>>) ->
        ^agg * uint64
        when ^agg : (static member Initial : ^agg)
        and ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))
        and ^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg))


/// <summary>基本聚合工厂模块
/// </summary>
[<RequireQualifiedAccess>]
module Basic =

    /// <summary>基本聚合工厂消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="Post">推送领域命令：领域命令值类型全名*跟踪ID*领域命令数据*返回聚合的管道。</param>
    /// <param name="Get">取出当前聚合。</param>
    type Msg<'agg> =
        | Post of string * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Get of AsyncReplyChannel<Result<'agg, string>>

    /// <summary>创建基本聚合工厂代理
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="reader">基于特定聚合，从某个版本开始获取领域事件的函数。</param>
    /// <param name="writer">基于特定聚合，领域事件流存储函数。</param>
    /// <param name="agg">聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="shot">或有的异步生成快照函数。</param>
    val inline internal agent :
        lg: DiagnoseLog.T ->
        reader: (uint64 -> (uint64 * string * ReadOnlyMemory<byte>) seq) ->
        writer: (uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) ->
        agg: ^agg ->
        version: uint64 ->
        shot: (^agg -> uint64 -> Async<unit>) option ->
        MailboxProcessor<Msg< ^agg>>
        when ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))
        and ^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg))
        and ^agg : (member Closed : bool)

    /// <summary>领域命令发往基本聚合工厂
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="agent">基本聚合工厂代理。</param>
    /// <param name="cvType">领域命令值类型全名。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="data">领域命令数据。</param>
    /// <param name="channel">返回聚合的管道。</param>
    val inline internal post :
        agent: MailboxProcessor<Msg< ^agg>> ->
        cvType: string ->
        traceId: string ->
        data: ReadOnlyMemory<byte> ->
        channel: AsyncReplyChannel<Result< ^agg, string>> ->
        unit

    /// <summary>取出当前聚合
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="agent">基本聚合工厂代理。</param>
    /// <param name="channel">返回聚合的管道。</param>
    val inline internal get :
        agent: MailboxProcessor<Msg< ^agg>> ->
        channel: AsyncReplyChannel<Result< ^agg, string>> ->
        unit


/// <summary>批处理聚合工厂模块
/// </summary>
[<RequireQualifiedAccess>]
module Batched =

    /// <summary>批处理聚合工厂消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="Add">添加领域命令：领域命令值类型全名*跟踪ID*领域命令数据*返回聚合的管道。</param>
    /// <param name="Launch">启动批处理。</param>
    /// <param name="Get">取出当前聚合。</param>
    type Msg<'agg> =
        | Add of string * string * ReadOnlyMemory<byte> * AsyncReplyChannel<Result<'agg, string>>
        | Launch of Timer
        | Get of AsyncReplyChannel<Result<'agg, string>>

    /// <summary>创建批处理聚合工厂代理
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="reader">基于特定聚合，从某个版本开始获取领域事件的函数。</param>
    /// <param name="writer">基于特定聚合，领域事件流存储函数。</param>
    /// <param name="interval">批处理间隔毫秒数。</param>
    /// <param name="agg">聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="shot">或有的异步生成快照函数。</param>
    val inline internal agent :
        lg: DiagnoseLog.T ->
        reader: (uint64 -> (uint64 * string * ReadOnlyMemory<byte>) seq) ->
        writer: (uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) ->
        interval: float ->
        agg: ^agg ->
        version: uint64 ->
        shot: (^agg -> uint64 -> Async<unit>) option ->
        MailboxProcessor<Msg< ^agg>>
        when ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))
        and ^agg : (member ApplyCommand : (string -> ReadOnlyMemory<byte> -> (string * ReadOnlyMemory<byte>) seq * ^agg))
        and ^agg : (member Closed : bool)

    /// <summary>领域命令发往批处理聚合工厂
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="agent">批处理聚合工厂代理。</param>
    /// <param name="cvType">领域命令值类型全名。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="data">领域命令数据。</param>
    /// <param name="channel">返回聚合的管道。</param>
    val inline internal post :
        agent: MailboxProcessor<Msg< ^agg>> ->
        cvType: string ->
        traceId: string ->
        data: ReadOnlyMemory<byte> ->
        channel: AsyncReplyChannel<Result< ^agg, string>> ->
        unit

    /// <summary>取出当前聚合
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="agent">批处理聚合工厂代理。</param>
    /// <param name="channel">返回聚合的管道。</param>
    val inline internal get :
        agent: MailboxProcessor<Msg< ^agg>> ->
        channel: AsyncReplyChannel<Result< ^agg, string>> ->
        unit


/// <summary>观察聚合工厂模块
/// </summary>
[<RequireQualifiedAccess>]
module Observed =

    /// <summary>观察聚合工厂消息类型
    /// </summary>
    /// <typeparam name="'agg">聚合类型。</typeparam>
    /// <param name="Append">附加领域事件：领域事件版本*领域事件类型*领域事件数据。</param>
    /// <param name="Get">取出当前聚合。</param>
    type Msg<'agg> =
        | Append of uint64 * string * ReadOnlyMemory<byte>
        | Get of AsyncReplyChannel<Result<'agg, string>>

    /// <summary>创建观察聚合工厂代理
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="lg">诊断日志记录器。</param>
    /// <param name="reader">基于特定聚合，从某个版本开始获取领域事件的函数。</param>
    /// <param name="agg">聚合。</param>
    /// <param name="version">聚合版本。</param>
    /// <param name="shot">或有的异步生成快照函数。</param>
    val inline internal agent :
        lg: DiagnoseLog.T ->
        reader: (uint64 -> (uint64 * string * ReadOnlyMemory<byte>) seq) ->
        agg: ^agg ->
        version: uint64 ->
        shot: (^agg -> uint64 -> Async<unit>) option ->
        MailboxProcessor<Msg< ^agg>>
        when ^agg : (member ApplyEvent : (string -> ReadOnlyMemory<byte> -> ^agg))

    /// <summary>领域事件附加到观察聚合工厂
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="agent">观察聚合工厂代理。</param>
    /// <param name="number">领域事件版本。</param>
    /// <param name="evType">领域事件类型。</param>
    /// <param name="data">领域事件数据。</param>
    val inline internal append :
        agent: MailboxProcessor<Msg< ^agg>> ->
        number: uint64 ->
        evType: string ->
        data: ReadOnlyMemory<byte> ->
        unit

    /// <summary>取出当前聚合
    /// </summary>
    /// <typeparam name="^agg">聚合类型。</typeparam>
    /// <param name="agent">观察聚合工厂代理。</param>
    /// <param name="channel">返回聚合的管道。</param>
    val inline internal get :
        agent: MailboxProcessor<Msg< ^agg>> ->
        channel: AsyncReplyChannel<Result< ^agg, string>> ->
        unit