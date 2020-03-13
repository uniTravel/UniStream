namespace Note.Application

open System
open UniStream.Domain
open Note.Domain


/// <summary>命令服务模块
/// <para>应用的命令服务内部实现，业务流程仅涉及单个聚合。</para>
/// </summary>
[<RequireQualifiedAccess>]
module internal CommandService =

    /// <summary>创建Actor
    /// </summary>
    /// <param name="actor">Actor聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    /// <returns>聚合值。</returns>
    val createActor : Aggregator.T<Actor.T> -> string -> Guid -> Guid -> CreateActor -> Async<Actor.Value>

    /// <summary>创建Note
    /// </summary>
    /// <param name="note">Note聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    val createNote : Aggregator.T<Note.T> -> string -> Guid -> Guid -> CreateNote -> Async<Note.Value>

    /// <summary>改变Note
    /// </summary>
    /// <param name="note">Note聚合器。</param>
    /// <param name="user">用户。</param>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">领域命令值。</param>
    val changeNote : Aggregator.T<Note.T> -> string -> Guid -> Guid -> ChangeNote -> Async<Note.Value>