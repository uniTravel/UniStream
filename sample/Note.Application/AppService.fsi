namespace Note.Application

open System
open Note


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
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">传入的领域命令值。</param>
    member CreateActor : Guid -> Guid -> Contract.CreateActor -> Async<Domain.Actor>

    /// <summary>创建Note
    /// </summary>
    /// <param name="aggId">聚合ID。</param>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">传入的领域命令值。</param>
    member CreateNote : Guid -> Guid -> Contract.CreateNote -> Async<Domain.Note>

    /// <summary>改变Note
    /// </summary>
    /// <param name="traceId">跟踪ID。</param>
    /// <param name="cv">传入的领域命令值。</param>
    member ChangeNote : Guid -> Contract.ChangeNote -> Async<Domain.Note>