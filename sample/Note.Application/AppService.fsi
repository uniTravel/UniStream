namespace Note.Application

open System
open UniStream.Infrastructure
open Note.Domain


/// <summary>应用服务类
/// <para>暴露给分布式服务。</para>
/// </summary>
[<Sealed>]
type AppService =

    /// <summary>构造函数
    /// </summary>
    /// <param name="reader">事件阅读者。</param>
    /// <param name="writer">事件撰写者。</param>
    /// <param name="ld">领域日志记录者。</param>
    /// <param name="lg">诊断日志记录者。</param>
    new : EventReader * EventWriter * DomainLogger * DiagnoseLogger -> AppService

    /// <summary>增加NoteObserver
    /// </summary>
    /// <param name="sub">事件订阅者。</param>
    member AddNoteObserver : Subscriber -> unit

    /// <summary>创建Actor
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">传入的领域命令值。</param>
    member CreateActor : string -> Guid -> Guid -> CreateActor -> Async<Actor.Value>

    /// <summary>创建Note
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">传入的领域命令值。</param>
    member CreateNote : string -> Guid -> Guid -> CreateNote -> Async<Note.Value>

    /// <summary>改变Note
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">传入的领域命令值。</param>
    member ChangeNote : string -> Guid -> Guid -> ChangeNote -> Async<Note.Value>

    /// <summary>批量改变Note
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">传入的领域命令值。</param>
    member BatchChangeNote : string -> Guid -> Guid -> ChangeNote -> Async<unit>

    /// <summary>获取Note
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    member GetNote : string -> Async<Note.Value>

    /// <summary>获取NoteObserver
    /// </summary>
    /// <param name="key">关联Key。</param>
    member GetNoteObserver : string -> Async<NoteObserver.Value[]>