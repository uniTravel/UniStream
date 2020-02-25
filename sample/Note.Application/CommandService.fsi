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
    /// <param name="cv">领域命令值类型。</param>
    val createActor : Aggregator.T<Actor.T> -> CreateActor -> Async<CreateActorReply>

    /// <summary>创建Note
    /// </summary>
    /// <param name="note">Note聚合器。</param>
    /// <param name="cv">领域命令值类型。</param>
    val createNote : Aggregator.T<Note.T> -> CreateNote -> Async<CreateNoteReply>

    /// <summary>改变Note
    /// </summary>
    /// <param name="note">Note聚合器。</param>
    /// <param name="cv">领域命令值类型。</param>
    val changeNote : Aggregator.T<Note.T> -> ChangeNote -> Async<ChangeNoteReply>