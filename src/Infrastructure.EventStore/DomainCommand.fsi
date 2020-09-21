namespace UniStream.Infrastructure.EventStore

open System
open EventStore.Client


/// <summary>领域命令访问者模块
/// <para>领域命令流存储的EventStore实现。有两种领域命令：</para>
/// <para>1、聚合命令：只影响一个聚合。</para>
/// <para>2、流程命令：会影响多个聚合。</para>
/// </summary>
[<RequireQualifiedAccess>]
module DomainCommand =

    /// <summary>写入领域命令
    /// <para>1、Stream名称：领域命令值类型全名。</para>
    /// <para>2、关联ID作为事件类型。</para>
    /// </summary>
    /// <typeparam name="^c">领域命令类型。</typeparam>
    /// <typeparam name="^v">领域命令响应类型。</typeparam>
    /// <param name="client">EventStore客户端。</param>
    /// <param name="timeout">以毫秒计的等待领域命令结果超时。</param>
    /// <param name="user">用户。</param>
    /// <param name="correlationId">关联ID：聚合命令为聚合键；流程命令为客户端标识。</param>
    /// <param name="cmd">领域命令。</param>
    /// <returns>聚合命令的执行结果。</returns>
    val inline launch :
        client: EventStoreClient ->
        timeout: int ->
        user: string ->
        correlationId: string ->
        cmd: ^c ->
        Async< ^v>
        when ^c : (static member FullName : string)