namespace Note.Application

open System
open UniStream.Domain
open Note.Contract
open Note.Domain


/// <summary>命令服务模块
/// <para>应用的命令服务内部实现，业务流程仅涉及单个聚合。</para>
/// </summary>
[<RequireQualifiedAccess>]
module CommandService =

    /// <summary>创建Actor
    /// </summary>
    /// <param name="actor">Actor聚合。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="cvType">领域命令值类型全名。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="data">领域命令数据。</param>
    /// <returns>聚合值。</returns>
    val createActor :
        actor: Immutable.T<Actor.T> ->
        user: string ->
        aggKey: string ->
        cvType: string ->
        traceId: string ->
        data: ReadOnlyMemory<byte> ->
        Async<Actor>

    /// <summary>创建Note
    /// </summary>
    /// <param name="note">Note聚合。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="cvType">领域命令值类型全名。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="data">领域命令数据。</param>
    val createNote :
        note: Mutable.T<Note.T> ->
        user: string ->
        aggKey: string ->
        cvType: string ->
        traceId: string ->
        data: ReadOnlyMemory<byte> ->
        Async<Note>

    /// <summary>改变Note
    /// </summary>
    /// <param name="note">Note聚合。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="cvType">领域命令值类型全名。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="data">领域命令数据。</param>
    val changeNote :
        note: Mutable.T<Note.T> ->
        user: string ->
        aggKey: string ->
        cvType: string ->
        traceId: string ->
        data: ReadOnlyMemory<byte> ->
        Async<Note>

    /// <summary>附加事件到NoteObserver
    /// </summary>
    /// <param name="obs">Note观察者聚合。</param>
    /// <param name="aggKey">聚合键。</param>
    /// <param name="number">领域事件版本。</param>
    /// <param name="evType">领域事件类型。</param>
    /// <param name="data">领域事件数据。</param>
    val appendNote :
        obs: Observer.T<NoteObserver.T> ->
        aggKey: string ->
        number: uint64 ->
        evType: string ->
        data: ReadOnlyMemory<byte> ->
        Async<unit>