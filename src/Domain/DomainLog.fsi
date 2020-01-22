namespace UniStream.Domain

open System


/// <summary>领域日志模块
/// </summary>
[<RequireQualifiedAccess>]
module DomainLog =

    /// <summary>领域日志
    /// </summary>
    [<CLIMutable>]
    type internal T

    /// <summary>领域日志记录器
    /// </summary>
    type Logger

    /// <summary>创建领域日志记录器
    /// </summary>
    /// <param name="aggType">领域类型全名。</param>
    /// <param name="logFunc">领域日志流存储函数。</param>
    val logger : string -> (string -> Guid -> string -> byte[] -> unit) -> Logger

    /// <summary>转成字节数组
    /// <para>领域日志数据采用二进制序列化。</para>
    /// </summary>
    /// <param name="log">领域日志数据。</param>
    /// <returns>领域日志数据的字节数组。</returns>
    val private asBytes : T -> byte[]

    /// <summary>转成领域日志数据
    /// <para>领域日志数据采用二进制反序列化。</para>
    /// </summary>
    /// <param name="bytes">领域日志数据的字节数组。</param>
    /// <returns>领域日志数据。</returns>
    val private fromBytes : byte[] -> T

    type Logger with

        /// <summary>记录Processing状态的领域日志
        /// </summary>
        /// <param name="aggId">聚合ID。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Process : Guid -> Guid -> Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Successed状态的领域日志
        /// </summary>
        /// <param name="aggId">聚合ID。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Success : Guid -> Guid -> Printf.StringFormat<'a, unit> -> 'a

        /// <summary>记录Failed状态的领域日志
        /// </summary>
        /// <param name="aggId">聚合ID。</param>
        /// <param name="traceId">跟踪ID。</param>
        /// <param name="format">字符串格式。</param>
        member Fail : Guid -> Guid -> Printf.StringFormat<'a, unit> -> 'a