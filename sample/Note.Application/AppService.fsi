namespace Note.Application

open System
open System.Threading.Tasks
open Note.Contract


/// <summary>应用服务类
/// <para>暴露给分布式服务。</para>
/// </summary>
[<Sealed>]
type AppService =

    /// <summary>构造函数
    /// </summary>
    /// <param name="es">领域事件流存储连接串。</param>
    /// <param name="ld">领域日志流存储连接串。</param>
    /// <param name="lg">诊断日志流存储连接串。</param>
    new : Uri * Uri * Uri -> AppService

    /// <summary>创建Actor
    /// </summary>
    /// <param name="cv">传入的领域命令值。</param>
    member CreateActor : CreateActor -> Task<CreateActorReply>

    /// <summary>创建Note
    /// </summary>
    /// <param name="cv">传入的领域命令值。</param>
    member CreateNote : CreateNote -> Task<CreateNoteReply>

    /// <summary>改变Note
    /// </summary>
    /// <param name="cv">传入的领域命令值。</param>
    member ChangeNote : ChangeNote -> Task<ChangeNoteReply>