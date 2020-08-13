namespace Note.Application

open System
open Note.Contract


/// <summary>Note应用服务
/// <para>暴露给分布式服务。</para>
/// </summary>
[<Sealed>]
type NoteService =

    /// <summary>构造函数
    /// </summary>
    /// <param name="reader">事件阅读者。</param>
    /// <param name="writer">事件撰写者。</param>
    /// <param name="ld">领域日志记录者。</param>
    /// <param name="lg">诊断日志记录者。</param>
    new :
        reader: (string -> string -> uint64 -> (uint64 * string * ReadOnlyMemory<byte>) seq) *
        writer: (string -> string -> uint64 -> (string * ReadOnlyMemory<byte> * Nullable<ReadOnlyMemory<byte>>) seq -> Async<unit>) *
        ld: (string -> string -> string -> ReadOnlyMemory<byte> -> Async<unit>) *
        lg: (string -> string -> ReadOnlyMemory<byte> -> Async<unit>) -> NoteService

    /// <summary>创建Actor
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    /// <returns>聚合值。</returns>
    member CreateActor :
        user: string ->
        aggKey: string ->
        traceId: string ->
        cv: CreateActor ->
        Async<Actor>

    /// <summary>创建Note
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    member CreateNote :
        user: string ->
        aggKey: string ->
        traceId: string ->
        cv: CreateNote ->
        Async<Note>

    /// <summary>改变Note
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    member ChangeNote :
        user: string ->
        aggKey: string ->
        traceId: string ->
        cv: ChangeNote ->
        Async<Note>

    /// <summary>创建Note
    /// <para>用于批处理。</para>
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    member BatchCreate :
        user: string ->
        aggKey: string ->
        traceId: string ->
        cv: CreateNote ->
        Async<Note>

    /// <summary>改变Note
    /// <para>用于批处理。</para>
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    member BatchChange :
        user: string ->
        aggKey: string ->
        traceId: string ->
        cv: ChangeNote ->
        Async<Note>

    /// <summary>附加事件到NoteObserver
    /// </summary>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="number">领域事件版本。</param>
    /// <param name="evType">领域事件类型。</param>
    /// <param name="data">领域事件数据。</param>
    member AppendNote :
        aggKey: string ->
        number: uint64 ->
        evType: string ->
        data: ReadOnlyMemory<byte> ->
        Async<unit>