namespace UniStream.Domain

open System


/// <summary>领域日志模块
/// </summary>
[<RequireQualifiedAccess>]
module DomainLog =

    /// <summary>领域日志记录器
    /// </summary>
    type T

    /// <summary>创建领域日志记录器
    /// </summary>
    /// <param name="aggType">聚合类型。</param>
    /// <param name="logFunc">领域日志流存储函数。</param>
    val logger :
        aggType: string ->
        logFunc: (string -> string -> ReadOnlyMemory<byte> -> Async<unit>) ->
        T

    type T with

        /// <summary>记录Processing状态的领域日志
        /// </summary>
        /// <param name="user">用户。</param>
        /// <param name="cvType">领域命令值类型。</param>
        /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Process :
            user: string ->
            cvType: string ->
            aggKey: string ->
            traceId: string ->
            format: Printf.StringFormat<'a, unit> ->
            'a

        /// <summary>记录Successed状态的领域日志
        /// </summary>
        /// <param name="user">用户。</param>
        /// <param name="cvType">领域命令值类型。</param>
        /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Success :
            user: string ->
            cvType: string ->
            aggKey: string ->
            traceId: string ->
            format: Printf.StringFormat<'a, unit> ->
            'a

        /// <summary>记录Failed状态的领域日志
        /// </summary>
        /// <param name="user">用户。</param>
        /// <param name="cvType">领域命令值类型。</param>
        /// <param name="aggKey">聚合键，源自GUID或者业务主键。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Fail :
            user: string ->
            cvType: string ->
            aggKey: string ->
            traceId: string ->
            format: Printf.StringFormat<'a, unit> ->
            'a