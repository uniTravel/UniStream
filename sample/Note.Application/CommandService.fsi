namespace Note.Application

open UniStream.Domain
open Note.Contract
open Note.Domain


/// <summary>命令服务模块
/// <para>应用的命令服务内部实现，业务流程仅涉及单个聚合。</para>
/// </summary>
[<RequireQualifiedAccess>]
module internal CommandService =

    /// <summary>创建Actor
    /// </summary>
    /// <param name="actor">Actor聚合器。</param>
    /// <param name="delta">边际影响。</param>
    val createActor : Aggregator.T<Actor.T> -> CreateActorCommand -> Async<CreateActorReply>

    /// <summary>创建Note
    /// </summary>
    /// <param name="note">Note聚合器。</param>
    /// <param name="delta">边际影响。</param>
    val createNote : Aggregator.T<Note.T> -> CreateNoteCommand -> Async<CreateNoteReply>

    /// <summary>改变Note
    /// </summary>
    /// <param name="note">Note聚合器。</param>
    /// <param name="delta">边际影响。</param>
    val changeNote : Aggregator.T<Note.T> -> ChangeNoteCommand -> Async<ChangeNoteReply>